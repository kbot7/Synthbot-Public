using RestSharp;
using Synthbot.Common;

namespace Synthbot.WebApp.Client
{
	public class SynthbotDiscordUserAuthenticator : ISynthbotAuthenticator
	{
		private readonly string _discordUserId;
		private readonly string _sharedSecret;
		private readonly string _discordUsername;
		public SynthbotDiscordUserAuthenticator(string discordUserId, string sharedSecret, string discordUsername)
		{
			_discordUserId = discordUserId;
			_sharedSecret = sharedSecret;
			_discordUsername = discordUsername;
		}
		public void Authenticate(IRestClient client, IRestRequest request)
		{
			string jwt = JwtBuilder.BuildDiscordJwt(_sharedSecret, _discordUserId, _discordUsername);
			request.AddHeader("Authorization", $"Bearer {jwt}");
		}
	}
}
