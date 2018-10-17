using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Synthbot.WebApp.Models
{
	public class SpotifyToken
	{
		[JsonProperty("access_token")]
		public string AccessToken { get; set; }
		[JsonProperty("expires_at")]
		public DateTime ExpiresAt { get; set; }
		[JsonProperty("refresh_token")]
		public string RefreshToken { get; set; }
		[JsonProperty("token_type")]
		public string TokenType { get; set; }

		public static SpotifyToken FromUserTokens(IEnumerable<IdentityUserToken<string>> userTokens)
		{
			var userTokenList = userTokens.ToList();

			var dateValue = userTokenList.FirstOrDefault(t => t.Name == "expires_at")?.Value;
			DateTime.TryParse(dateValue, out DateTime parsedDate);

			var token = new SpotifyToken()
			{
				AccessToken =  userTokenList.FirstOrDefault(t => t.Name == "access_token")?.Value,
				RefreshToken = userTokenList.FirstOrDefault(t => t.Name == "refresh_token")?.Value,
				ExpiresAt = parsedDate
			};
			return token;
		}

		public void Validate()
		{
			if (AccessToken == null || string.IsNullOrWhiteSpace(AccessToken)) throw new ArgumentNullException(nameof(AccessToken));
			if (RefreshToken == null || string.IsNullOrWhiteSpace(RefreshToken)) throw new ArgumentNullException(nameof(RefreshToken));
			if (ExpiresAt == null) throw new ArgumentNullException(nameof(ExpiresAt));
		}
	}
}
