using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synthbot.DAL;
using Synthbot.DAL.Models;
using Synthbot.WebApp.Models;

namespace Synthbot.WebApp.Services
{
	// Refer: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-2.1
	//			Details using in memory caches in asp.net core

	/// <summary>
	/// Should be Scoped
	/// </summary>
	public class UserTokenService
	{
		private readonly IMemoryCache _memoryCache;
		private readonly ApplicationDbContext _db;
		private readonly IConfiguration _config;
		private readonly SpotifyTokenRefreshService _refreshService;
		private readonly SemaphoreSlim _dbSignal;
		private readonly ILogger<UserTokenService> _logger;
		public UserTokenService(
			IMemoryCache memoryCache,
			ApplicationDbContext db,
			IConfiguration config,
			SpotifyTokenRefreshService refreshService,
			ILogger<UserTokenService> logger)
		{
			_memoryCache = memoryCache;
			_db = db;
			_config = config;
			_refreshService = refreshService;
			_dbSignal = new SemaphoreSlim(1, 1);
			_logger = logger;
		}

		public async Task<SpotifyUserToken> GetTokenFromSpotifyUserIdAsync(string spotifyUserId)
		{
			var userId = await _db.UserLogins
				.Where(ul => ul.LoginProvider == "Spotify")
				.Select(ul => ul.UserId)
				.FirstOrDefaultAsync();

			return await GetTokenAsync(userId);
		}
		public Task<SpotifyUserToken> GetTokenAsync(SynthbotUser user) => GetTokenAsync(user.Id);
		public async Task<SpotifyUserToken> GetTokenAsync(string synthbotUserId)
		{
			_logger.Log(LogLevel.Information, "Getting Spotify token for user: {synthbotUserId}", synthbotUserId);
			var isCached = _memoryCache.TryGetValue(synthbotUserId, out var cachedTokenObj);
			if (isCached)
			{
				var cachedToken = (SpotifyUserToken)cachedTokenObj;
				_logger.Log(LogLevel.Information, "Found cached Spotify token for user: {synthbotUserId}, token expiration: {}", synthbotUserId, cachedToken.SpotifyAccessTokenExpiry.ToString("O"));

				if (cachedToken.ShouldRefresh())
				{
					return await RefreshToken(cachedToken, synthbotUserId);
				}
				return cachedToken;
			}
			else
			{
				_logger.Log(LogLevel.Information, "Spotify token not found in cache. Getting from db and caching it for user: {synthbotUserId}", synthbotUserId);
				var token = await GetDbToken(synthbotUserId);
				if (token.ShouldRefresh())
				{
					token = await RefreshToken(token, synthbotUserId);
				}

				_memoryCache.Set<SpotifyUserToken>(synthbotUserId, token);
				return token;
			}
		}

		public async Task<SpotifyUserToken> RefreshToken(SpotifyUserToken userToken, string synthbotUserId)
		{
			_logger.Log(LogLevel.Information, "Refreshing spotify token for user: {synthbotUserId}", synthbotUserId);
			var refreshResponse = await userToken.Refresh(_refreshService);

			if (!refreshResponse.Success)
			{
				_logger.Log(LogLevel.Error, "Token refresh failed for user: {synthbotUserId}", synthbotUserId);
				return null;
			}

			_logger.Log(LogLevel.Information, "Token refresh successful for user: {synthbotUserId}", synthbotUserId);
			// Update the token in the DB for future auth
			await UpdateDbAsync(userToken);
			return userToken;
		}

		private async Task<SpotifyUserToken> GetDbToken(string synthbotUserId)
		{
			var spotifyUserId = await _db.UserLogins
				.Where(ul => ul.LoginProvider == "Spotify")
				.Select(ul => ul.ProviderKey)
				.FirstOrDefaultAsync();

			if (string.IsNullOrEmpty(spotifyUserId))
			{
				return null;
			}

			var userTokensQueryable = _db.UserTokens.Where(ut => ut.UserId == synthbotUserId && ut.LoginProvider == "Spotify");

			var spotifyToken = SpotifyToken.FromUserTokens(userTokensQueryable);
			spotifyToken.Validate();

			var userToken = new SpotifyUserToken()
			{
				SynthbotUserId = synthbotUserId,
				SpotifyUserId = spotifyUserId,
				SpotifyAccessToken = spotifyToken.AccessToken,
				SpotifyAccessTokenExpiry = spotifyToken.ExpiresAt,
				SpotifyRefreshToken = spotifyToken.RefreshToken
			};

			return userToken;
		}


		private async Task UpdateDbAsync(SpotifyUserToken userToken)
		{
			await _dbSignal.WaitAsync();

			var accessTokenRecord = await _db.UserTokens
				.FirstOrDefaultAsync(t => t.UserId == userToken.SynthbotUserId && t.Name == "access_token");
			if (accessTokenRecord != null)
			{
				accessTokenRecord.Value = userToken.SpotifyAccessToken;
				_db.Update(accessTokenRecord);
			}
			else
			{
				accessTokenRecord = new IdentityUserToken<string>()
				{
					LoginProvider = "Spotify",
					Name = "access_token",
					UserId = userToken.SynthbotUserId,
					Value = userToken.SpotifyAccessToken

				};
				await _db.UserTokens.AddAsync(accessTokenRecord);
			}
			var expiresAtRecord = await _db.UserTokens.FirstOrDefaultAsync(t => t.UserId == userToken.SynthbotUserId && t.Name == "expires_at");
			if (expiresAtRecord != null)
			{
				expiresAtRecord.Value = userToken.SpotifyAccessTokenExpiry.ToString("O");
				_db.Update(expiresAtRecord);
			}
			else
			{
				expiresAtRecord = new IdentityUserToken<string>()
				{
					LoginProvider = "Spotify",
					Name = "expires_at",
					UserId = userToken.SynthbotUserId,
					Value = userToken.SpotifyAccessTokenExpiry.ToString("O")
				};
				await _db.AddAsync(expiresAtRecord);
			}

			try
			{
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();
			}

		}

		
	}

	public class SpotifyUserToken
	{
		public string SynthbotUserId { get; set; }
		public string SpotifyUserId { get; set; }
		public string SpotifyAccessToken { get; set; }
		public DateTime SpotifyAccessTokenExpiry { get; set; }
		public string SpotifyRefreshToken { get; set; }

		public bool ShouldRefresh(int minuteTolerance = 5) =>
			DateTime.UtcNow > SpotifyAccessTokenExpiry.AddMinutes(minuteTolerance * -1);

		public Task<RefreshAccessTokenResponse> Refresh(SpotifyTokenRefreshService refreshService)
		{
			return refreshService.RefreshTokenAsync(this);
		}
	}
}
