namespace Synthbot.Common.Authentication
{
	public class TokenPayload
	{
		public string DiscordUserId { get; set; }
		public string SpotifyUserId { get; set; }
		public string SpotifyAccessToken { get; set; }
		public double ExpirationMs { get; set; }
	}
}
