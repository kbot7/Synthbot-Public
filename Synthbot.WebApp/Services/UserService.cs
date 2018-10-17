using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Synthbot.DAL;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;

namespace Synthbot.WebApp.Services
{
	public class UserService
	{
		// TODO any calls to _db should be moved to a UserRepository layer
		private readonly ApplicationDbContext _db;
		private readonly UserManager<SynthbotUser> _userManager;
		private readonly IConfiguration _config;
		private readonly UserIdCache _cache;
		private readonly SemaphoreSlim _dbSignal;
		private readonly DiscordUserRepository _discordUserRepo;
		public UserService(
			ApplicationDbContext db,
			UserManager<SynthbotUser> userMgr,
			IConfiguration config,
			UserIdCache cache,
			DiscordUserRepository discordUserRepo)
		{
			_db = db;
			_userManager = userMgr;
			_config = config;
			_cache = cache;
			_discordUserRepo = discordUserRepo;
			_dbSignal = new SemaphoreSlim(1, 1);
		}

		public async Task<SynthbotUser> GetUserBySynthbotUserIdAsync(string synthbotUserId)
		{
			return await _userManager.FindByIdAsync(synthbotUserId);
		}
		public async Task<string> GetUserIdByDiscordIdAsync(string discordUserId)
		{
			var cached = _cache.FromDiscordId(discordUserId);
			if (cached != null && !string.IsNullOrWhiteSpace(cached.SynthbotId))
			{
				return cached.SynthbotId;
			}
			else
			{
				var synthbotId = await _db.Users
					.Where(u => u.DiscordUserId == discordUserId)
					.Select(u => u.Id).FirstOrDefaultAsync();

				if (!string.IsNullOrWhiteSpace(synthbotId))
				{
					_cache.Add(synthbotId: synthbotId, discordId: discordUserId);
					return synthbotId;
				}
			}
			return null;
		}

		public async Task<SynthbotUser> GetUserByDiscordIdAsync(string discordUserId)
		{
			var cached = _cache.FromDiscordId(discordUserId);
			if (cached != null &&  !string.IsNullOrWhiteSpace(cached.SynthbotId))
			{
				return await _db.Users.FirstOrDefaultAsync(u => u.Id == cached.SynthbotId);
			}
			else
			{
				var user = await _db.Users.FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId);
				_cache.Add(synthbotId: user.Id, discordId: discordUserId);
				return user;
			}
		}

		public async Task<string> GetSpotifyIdFromUserIdAsync(string userId)
		{
			var cached = _cache.FromSynthbotId(userId);
			if (cached != null && string.IsNullOrWhiteSpace(cached.SpotifyId))
			{
				return cached.SpotifyId;
			}
			else
			{
				var spotifyId = await _db.UserLogins
					.Where(ul => ul.UserId == userId && ul.LoginProvider == "Spotify")
					.Select(ul => ul.ProviderKey)
					.FirstOrDefaultAsync();
				if (!string.IsNullOrWhiteSpace(spotifyId))
				{
					_cache.Add(synthbotId:userId, spotifyId:spotifyId);
				}
			}
			return null;
		}

		public async Task<string> GetUserIdBySpotifyIdAsync(string spotifyId)
		{
			var cached = _cache.FromSpotifyId(spotifyId);
			if (cached != null && cached.SynthbotId != null)
			{
				return cached.SynthbotId;
			}
			else
			{
				return (await GetUserIdBySpotifyIdAsync(spotifyId));
			}
		}

		public async Task SetPlaybackSession(SynthbotUser user, PlaybackSession session)
		{
			await _dbSignal.WaitAsync();

			try
			{
				_db.Users.Attach(user);
				user.ActivePlaybackSession = session;
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();
			}
		}

		public async Task RemovePlaybackSession(SynthbotUser user)
		{
			await _dbSignal.WaitAsync();

			try
			{
				_db.Users.Attach(user);
				user.ActivePlaybackSession = null;
				user.ActivePlaybackSessionId = null;
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();
			}
		}

		public async Task SetAutoJoin(SynthbotUser user, bool opt)
		{
			await _dbSignal.WaitAsync();

			try
			{
				_db.Users.Attach(user);
				user.AutoJoin = opt;
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();
			}
		}

		public async Task SetDefaultDevice(SynthbotUser user, string defaultDevice)
		{
			await _dbSignal.WaitAsync();

			try
			{
				_db.Users.Attach(user);
				user.DefaultSpotifyDevice = defaultDevice;
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();
			}
		}

		public async Task<SynthbotUser> GetUserBySpotifyIdAsync(string spotifyUserId)
		{
			var cached = _cache.FromSpotifyId(spotifyUserId);
			if (cached != null && !string.IsNullOrWhiteSpace(cached.SynthbotId))
			{
				return await _db.Users.FirstOrDefaultAsync(u => u.Id == cached.SynthbotId);
			}
			else
			{
				var user = await _userManager.FindByLoginAsync("Spotify", spotifyUserId);
				_cache.Add(synthbotId: user.Id, spotifyId: spotifyUserId);
				return user;
			}
		}

	}
}
