using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Synthbot.Common;
using Synthbot.WebApp.Services;

namespace Synthbot.WebApp
{
	public class DiscordBotClientJwtBearerEvents : JwtBearerEvents
	{
		private readonly ILogger<DiscordBotClientJwtBearerEvents> _logger;
		private readonly UserService _userService;
		public DiscordBotClientJwtBearerEvents(
			ILogger<DiscordBotClientJwtBearerEvents> logger,
			UserService userService)
		{
			_logger = logger;

			_userService = userService;

			this.OnTokenValidated = OnTokenValidatedFunc;
			this.OnAuthenticationFailed = OnAuthenticationFailedFunc;
			this.OnChallenge = OnChallengeFunc;
			this.OnMessageReceived = OnMessageReceivedFunc;
		}

		private async Task OnTokenValidatedFunc(TokenValidatedContext arg)
		{
			// Set SynthbotUser Id in the claims if the token from the DiscordBot contains a user id
			var discordUserId = arg.Principal.GetDiscordUserId();
			if (!string.IsNullOrWhiteSpace(discordUserId))
			{
				var userId = await _userService.GetUserIdByDiscordIdAsync(discordUserId);
				if (string.IsNullOrWhiteSpace(userId))
				{
					_logger.Log(LogLevel.Information, "DiscordUserId: {0} was not registered", discordUserId);
				}
				else
				{
					var userIdClaim = new Claim(ClaimTypes.NameIdentifier, userId);
					((ClaimsIdentity)arg.Principal.Identity).AddClaim(userIdClaim);
				}
			}
		}

		// Including these as placeholders for convenient future use
		private Task OnMessageReceivedFunc(MessageReceivedContext arg)
		{
			return Task.CompletedTask;
		}

		private Task OnChallengeFunc(JwtBearerChallengeContext arg)
		{
			return Task.CompletedTask;
		}

		private Task OnAuthenticationFailedFunc(AuthenticationFailedContext arg)
		{
			return Task.CompletedTask;
		}
	}
}
