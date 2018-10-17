using System.Linq;
using System.Security.Claims;
using Synthbot.Common.Authentication;

namespace Synthbot.Common
{
	public static class DiscordClaimsExtensions
	{
		public static string GetClaimValueFromType(this ClaimsPrincipal principal, string type)
		{
			return principal.Claims.FirstOrDefault(c => c.Type == type)?.Value;
		}
		public static Claim GetClaimFromType(this ClaimsPrincipal principal, string type)
		{
			return principal.Claims.FirstOrDefault(c => c.Type == type);
		}

		public static string GetDiscordUserId(this ClaimsPrincipal principal)
		{
			return principal.GetClaimValueFromType(SynthbotClaimTypes.DiscordUserId);
		}
		public static Claim GetDiscordUserIdClaim(this ClaimsPrincipal principal)
		{
			return principal.GetClaimFromType(SynthbotClaimTypes.DiscordUserId);
		}
		public static string GetDiscordBotUserId(this ClaimsPrincipal principal)
		{
			return principal.GetClaimValueFromType(SynthbotClaimTypes.DiscordBotMachineId);
		}
		public static string GetDiscordUsername(this ClaimsPrincipal principal)
		{
			return principal.GetClaimValueFromType(SynthbotClaimTypes.DiscordUsername);
		}

		public static Claim GetDiscordUsernameClaim(this ClaimsPrincipal principal)
		{
			return principal.GetClaimFromType(SynthbotClaimTypes.DiscordUsername);
		}

		public static string GetHubReplyUserId(this ClaimsPrincipal principal)
		{
			return principal.GetClaimValueFromType(SynthbotClaimTypes.AppSignalRUser);
		}
	}
}
