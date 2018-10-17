using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Synthbot.Common.Authentication;

namespace Synthbot.Common
{
	public static class JwtBuilder
	{
		public static string BuildDiscordJwt(string sharedSecret, string discordUserId, string discordUserName)
		{
			// Create Security key  using private key above:
			// not that latest version of JWT using Microsoft namespace instead of System
			var securityKey = new Microsoft
				.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(sharedSecret));

			// Also note that securityKey length should be >256b
			// so you have to make sure that your private key has a proper length
			//
			var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials
							(securityKey, SecurityAlgorithms.HmacSha256Signature);

			//	Finally create a Token
			var header = new JwtHeader(credentials);

			//Some PayLoad that contain information about the  customer
			var claims = new Claim[]
			{
				new Claim(SynthbotClaimTypes.DiscordUserId, discordUserId),
				new Claim(SynthbotClaimTypes.DiscordUsername, discordUserName),
				new Claim(SynthbotClaimTypes.AppSignalRUser, "Synthbot.DiscordBot.SignalR.User")
			};
			var payload = new JwtPayload("Synthbot.DiscordBot", "Synthbot.WebApp", claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow);

			//
			var secToken = new JwtSecurityToken(header, payload);
			var handler = new JwtSecurityTokenHandler();

			// Token to String so you can use it in your client
			var tokenString = handler.WriteToken(secToken);

			return tokenString;
		}

		public static string BuildDiscordJwtForSignalR(string sharedSecret, string signalrUserId)
		{
			// Create Security key  using private key above:
			// not that latest version of JWT using Microsoft namespace instead of System
			var securityKey = new Microsoft
				.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(sharedSecret));

			// Also note that securityKey length should be >256b
			// so you have to make sure that your private key has a proper length
			//
			var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials
				(securityKey, SecurityAlgorithms.HmacSha256Signature);

			//  Finally create a Token
			var header = new JwtHeader(credentials);

			//Some PayLoad that contain information about the  customer
			var claims = new Claim[]
			{
				new Claim(ClaimTypes.NameIdentifier, signalrUserId),
			};
			var payload = new JwtPayload("Synthbot.DiscordBot", "Synthbot.WebApp", claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow);

			//
			var secToken = new JwtSecurityToken(header, payload);
			var handler = new JwtSecurityTokenHandler();

			// Token to String so you can use it in your client
			var tokenString = handler.WriteToken(secToken);

			return tokenString;
		}
	}
}
