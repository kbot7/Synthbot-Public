using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using Synthbot.Common;
using Synthbot.WebApp.Services;

namespace Synthbot.WebApp
{
	public class SpotifyHttpClientFactory
	{
		// TODO technically, injecting the ServiceProvider is the 'Service Locator' anti-pattern. Not seeing a better way to do this at the moment
		private readonly IServiceProvider _services;
		private readonly IConfiguration _config;
		public SpotifyHttpClientFactory(IServiceProvider services, IConfiguration config)
		{
			_services = services;
			_config = config;
		}

		public async Task<SpotifyWebAPI> CreateClientFromContext(HttpContext context)
		{
			var userId = context?
				.User?
				.GetClaimValueFromType(ClaimTypes.NameIdentifier);
			return await CreateUserClientAsync(userId);
		}

		public async Task<SpotifyWebAPI> CreateUserClientAsync(string synthbotUserId)
		{
			var client = _services.GetRequiredService<SpotifyWebAPI>();

			using (var scope = _services.CreateScope())
			{
				var tokenService = scope.ServiceProvider.GetRequiredService<UserTokenService>();
				var token = await tokenService.GetTokenAsync(synthbotUserId);
				client.AccessToken = token.SpotifyAccessToken;
				client.TokenType = "Bearer";
			}
			return client;
		}

		public async Task<SpotifyWebAPI> CreateAppClient()
		{
			var client = _services.GetRequiredService<SpotifyWebAPI>();

			var spotifyClientCredentialsAuth = new SpotifyClientCredentialsAuth(
				_config["spotify.api.clientid"],
				_config["spotify.api.clientsecret"]);

			var token = await spotifyClientCredentialsAuth.GetToken();
			client.AccessToken = token.AccessToken;
			client.TokenType = "Bearer";

			return client;
		}
	}
}
