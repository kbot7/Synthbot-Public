using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpotifyAPI.Web.Models;

namespace Synthbot.WebApp
{
	public class SpotifyClientCredentialsAuth
	{
		public string ClientSecret { get; set; }

		public string ClientId { get; set; }

		public SpotifyClientCredentialsAuth(string clientId, string clientSecret)
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
		}

		public async Task<Token> GetToken()
		{
			string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + ClientSecret));

			List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
			{
				new KeyValuePair<string, string>("grant_type", "client_credentials")
			};

			HttpClient client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");
			HttpContent content = new FormUrlEncodedContent(args);

			HttpResponseMessage resp = await client.PostAsync("https://accounts.spotify.com/api/token", content);
			string msg = await resp.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<Token>(msg);
		}
	}
}
