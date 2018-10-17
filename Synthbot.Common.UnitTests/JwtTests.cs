using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Synthbot.Common.Authentication;
using Xunit;

namespace Synthbot.Common.UnitTests
{
	public class JwtTests
	{
		[Fact]
		public void DiscordJwtTest()
		{
			// Arrange
			ulong discordUserId = 123;
			string discordUsername = "Example Discord Username";
			// This is a completely arbitrary string and can be safely committed 
			string sharedSecret = "5g35gq3095gq3598jgq30598jgq3950gjh48079ghq30954yhgq349875hgq9754hqg78946qgh45gqjn4rjigerg89w5w9845ug89w45g9w45gw";
			var jwtString = JwtBuilder.BuildDiscordJwt(sharedSecret, discordUserId.ToString(), discordUsername);
			var securityKey = new Microsoft
				.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(sharedSecret));

			var handler = new JwtSecurityTokenHandler();
			handler.ValidateToken(jwtString, new TokenValidationParameters()
			{
				ValidIssuer = "Synthbot.DiscordBot",
				ValidAudience = "Synthbot.WebApp",
				ValidateIssuer = true,
				ValidateAudience = true,
				RequireSignedTokens = true,
				IssuerSigningKey = securityKey
			}, out SecurityToken validatedToken);

			var validatedJwtToken = (JwtSecurityToken)validatedToken;
			
			Assert.Equal(discordUserId.ToString(), validatedJwtToken.Claims.FirstOrDefault(c => c.Type == SynthbotClaimTypes.DiscordUserId)?.Value);
		}
	}
}
