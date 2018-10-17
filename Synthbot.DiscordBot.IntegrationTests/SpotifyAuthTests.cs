using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using Xunit;

namespace Synthbot.DiscordBot.IntegrationTests
{
	public class SpotifyAuthTests
	{
		[Fact]
		public void Spotify_Auth_ClientCredentialsTest()
		{
			var auth = new CredentialsAuth(
				ConfigurationProvider.Config["spotify.api.clientid"],
				ConfigurationProvider.Config["spotify.api.clientsecret"]);
			Token token = auth.GetToken().Result;
			var spotifyApi = new SpotifyWebAPI
			{
				AccessToken = token.AccessToken,
				TokenType = token.TokenType,
				UseAuth = true
			};

			// 0UF7XLthtbSF2Eur7559oV is Kavinsky's Artist ID
			var artist = spotifyApi.GetArtist("0UF7XLthtbSF2Eur7559oV");

			Assert.NotNull(artist);
			Assert.Equal("Kavinsky", artist.Name);
		}
	}
}
