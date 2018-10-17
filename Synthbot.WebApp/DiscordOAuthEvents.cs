using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Synthbot.WebApp.Hubs;

namespace Synthbot.WebApp
{
	public class DiscordOAuthEvents : OAuthEvents
	{
		private readonly ILogger<DiscordOAuthEvents> _logger;
		private readonly IHubContext<DiscordBotHub> _botHub;
		private readonly IConfiguration _config;
		public DiscordOAuthEvents(ILogger<DiscordOAuthEvents> logger, IHubContext<DiscordBotHub> botHub, IConfiguration config)
		{
			_logger = logger;
			_botHub = botHub;
			_config = config;

			this.OnCreatingTicket = OnCreatingTicketFuncAsync;
			this.OnTicketReceived = OnTicketReceivedFunc;
			this.OnRedirectToAuthorizationEndpoint = OnRedirectToAuthorizationEndpointFunc;
			this.OnRemoteFailure = OnRemoteFailureFunc;
		}

		public Task OnCreatingTicketFuncAsync(OAuthCreatingTicketContext ctx)
		{
			// Get "State" property
			ctx.Properties.Items.TryGetValue("state", out var stateValue);
			if (string.IsNullOrWhiteSpace(stateValue))
			{
				// Fail auth if state property is missing
				ctx.Fail("Discord Token JWT was missing in the 'State' parameter of the authentication request");
				return Task.CompletedTask;
			}

			ctx.Principal.Identities.First().AddClaim(new Claim("ReferralTokenId", stateValue));
			return Task.CompletedTask;
		}

		public Task OnTicketReceivedFunc(TicketReceivedContext ctx)
		{
			return Task.CompletedTask;
		}

		private Task OnRemoteFailureFunc(RemoteFailureContext arg)
		{
			return Task.CompletedTask;
		}

		private Task OnRedirectToAuthorizationEndpointFunc(RedirectContext<OAuthOptions> ctx)
		{
			ctx.Response.Redirect(ctx.RedirectUri);
			return Task.CompletedTask;
		}

		private ClaimsPrincipal GetClaimsFromStateJwt(string stateJwt)
		{
			var key = Encoding.ASCII.GetBytes(_config["synthbot.token.sharedsecret"]);
			var handler = new JwtSecurityTokenHandler();
			var validations = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidAudiences = new[] { "Synthbot.WebApp" },
				ValidIssuers = new[] { "Synthbot.DiscordBot" }
			};
			ClaimsPrincipal claims = null;
			try
			{
				claims = handler.ValidateToken(stateJwt, validations, out SecurityToken tokenSecure);
			}
			catch (Exception ex)
			{
				_logger.Log(LogLevel.Error, "State Token validation failed", ex);
			}

			return claims;
		}

	}
}
