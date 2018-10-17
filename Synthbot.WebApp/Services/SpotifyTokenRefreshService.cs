using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Synthbot.WebApp.Models;

namespace Synthbot.WebApp.Services
{
	/// <summary>
	/// Register as a singleton
	/// </summary>
	public class SpotifyTokenRefreshService : IDisposable
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _config;
		public SpotifyTokenRefreshService(IConfiguration config, HttpClient httpClient)
		{
			_config = config;
			_httpClient = httpClient;
		}

		public async Task<RefreshAccessTokenResponse> RefreshTokenAsync(SpotifyUserToken userToken)
		{
			var uri = new Uri($"https://accounts.spotify.com/api/token");

			// Create body parameters
			var parameters = new List<KeyValuePair<string, string>>();
			parameters.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
			parameters.Add(new KeyValuePair<string, string>("refresh_token", userToken.SpotifyRefreshToken));

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new FormUrlEncodedContent(parameters) };

			// Create encoded credentials header value
			var sb = new StringBuilder();
			sb.Append(_config["spotify.api.clientid"]);
			sb.Append($":");
			sb.Append(_config["spotify.api.clientsecret"]);
			var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));

			// Add authorization header
			request.Headers.Add("Authorization", $"Basic {encodedCredentials}");

			// Send the request
			var result = await _httpClient.SendAsync(request);

			if (result.IsSuccessStatusCode)
			{
				var contentString = await result.Content.ReadAsStringAsync();
				var tokenResponse = JsonConvert.DeserializeObject<SpotifyAccessTokenResponse>(contentString);

				// Set the updated access token on the user token that was passed in
				userToken.SpotifyAccessToken = tokenResponse.AccessToken;
				userToken.SpotifyAccessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds);

				// Return token response
				return RefreshAccessTokenResponse.CreateSuccess();
			}
			else
			{
				return RefreshAccessTokenResponse
					.CreateFailure(
						$"Failed to refresh token. Received error response from Spotify. Status Code: {result.StatusCode}");
			}
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}

		private class SpotifyAccessTokenResponse
		{
			[JsonProperty("access_token")]
			public string AccessToken { get; set; }
			[JsonProperty("expires_in")]
			public int ExpiresInSeconds { get; set; }
		}
	}
}
