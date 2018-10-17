using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using Synthbot.DAL.Models;

namespace Synthbot.WebApp.Client
{
	public class SynthbotRestClient : RestClient
	{
		public SynthbotRestClient(Uri baseUri, ISynthbotAuthenticator authenticator) : base(baseUri)
		{
			if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
			Authenticator = authenticator ?? throw new ArgumentException(nameof(authenticator));
		}

		public Task<PlaybackSessionInfo> GetCurrentPlayback(string voiceChannelId)
		{
			var request = new RestRequest("api/SynthChannel/GetCurrentPlayback", Method.GET);
			request.AddParameter("voiceChannelId", voiceChannelId);

			var response = Execute<PlaybackSessionInfo>(request);

			if (response.IsSuccessful)
			{
				var body = response.Data;
				return Task.FromResult(body);
			}

			return null;
		}

		public Task<bool> AddUserToChannel(string discordChannelId)
		{
			var request = new RestRequest("api/SynthChannel/AddUser", Method.GET);
			request.AddParameter("voiceChannelId", discordChannelId);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<bool> RemoveUserFromChannel(string discordChannelId)
		{
			var request = new RestRequest("api/SynthChannel/RemoveUser", Method.GET);
			request.AddParameter("voiceChannelId", discordChannelId);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<string> GetSpotifyToken()
		{
			var request = new RestRequest("api/SynthChannel/SpotifyAccessToken", Method.GET);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				var body = (string)JsonConvert.DeserializeObject(response.Content);
				return Task.FromResult(body);
			}

			return Task.FromResult("");
		}

		public Task<PlayPauseInfo> PausePlayback(string discordChannelId)
		{
			var request = new RestRequest("api/SynthChannel/PauseChannel", Method.GET);
			request.AddParameter("voiceChannelId", discordChannelId);

			var response = Execute<PlayPauseInfo>(request);

			if (response.IsSuccessful)
			{
				var body = response.Data;
				return Task.FromResult(body);
			}
			return null;
		}

		public Task<PlayPauseInfo> ResumePlayback(string discordChannelId)
		{
			var request = new RestRequest("api/SynthChannel/ResumeChannel", Method.GET);
			request.AddParameter("voiceChannelId", discordChannelId);

			var response = Execute<PlayPauseInfo>(request);

			if (response.IsSuccessful)
			{
				var body = response.Data;
				return Task.FromResult(body);
			}
			return null;
		}

		public Task<SkipInfo> Skip(string discordChannelId)
		{
			var request = new RestRequest("api/SynthChannel/Skip", Method.GET);
			request.AddParameter("voiceChannelId", discordChannelId);

			var response = Execute<SkipInfo>(request);

			if (response.IsSuccessful)
			{
				var body = response.Data;
				return Task.FromResult(body);
			}
			return null;
		}

		public Task<ChangePlaylistInfo> ChangePlaylist(string discordChannelId, string newSpotifyPlaylistId)
		{
			var request = new RestRequest("api/SynthChannel/ChangePlaylist", Method.GET);
			request.AddParameter("voiceChannelId", discordChannelId);
			request.AddParameter("newSpotifyPlaylistId", newSpotifyPlaylistId);

			var response = Execute<ChangePlaylistInfo>(request);

			if (response.IsSuccessful)
			{
				var body = response.Data;
				return Task.FromResult(body);
			}

			return Task.FromResult(new ChangePlaylistInfo());
		}

		public Task<bool> IsUserRegistered(string discordUserId)
		{
			var request = new RestRequest("api/SynthChannel/IsUserRegistered", Method.GET);
			request.AddParameter("discordUserId", discordUserId);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				var body = response.Content;
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<bool> SetAutoJoin(string discordUserId, bool autoJoin)
		{
			var request = new RestRequest("api/SynthChannel/SetAutoJoin", Method.GET);
			request.AddParameter("discordUserId", discordUserId);
			request.AddParameter("autoJoin", autoJoin);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				var body = response.Content;
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<bool> SetDeviceId(string discordUserId, string spotifyDeviceId)
		{
			var request = new RestRequest("api/SynthChannel/SetDevice", Method.GET);
			request.AddParameter("discordUserId", discordUserId);
			request.AddParameter("spotifyDeviceId", spotifyDeviceId);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				var body = response.Content;
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<bool> CanAutoJoin(string discordUserId)
		{
			var request = new RestRequest("api/SynthChannel/CanAutoJoin", Method.GET);
			request.AddParameter("discordUserId", discordUserId);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				var body = response.Content;
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<bool> SetTextUpdateChannel(string voiceChannelId, string textChannelDiscordId)
		{
			var request = new RestRequest("api/SynthChannel/SetTextUpdateChannel", Method.GET);
			request.AddParameter("voiceChannelId", voiceChannelId);
			request.AddParameter("textChannelDiscordId", textChannelDiscordId);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				var body = response.Content;
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<bool> Reset()
		{
			var request = new RestRequest("api/SynthChannel/Reset", Method.GET);

			var response = Execute(request);

			if (response.IsSuccessful)
			{
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public async Task<SynthbotUser> GetUserFromDiscordIdAsync(string discordId)
		{
			var request = new RestRequest("api/SynthChannel/GetSynthbotUserFromDiscordId", Method.GET);
			request.AddParameter("discordId", discordId);

			var response = await ExecuteGetTaskAsync<SynthbotUser>(request);

			if (response.IsSuccessful)
			{
				return response.Data;
			}

			return null;
		}

		public async Task<DiscordUserStatus> GetDiscordUserStatus(string discordUserId)
		{
			var request = new RestRequest("api/DiscordUser/GetStatus", Method.GET);
			request.AddParameter("discordUserId", discordUserId);

			var response = await ExecuteGetTaskAsync<DiscordUserStatus>(request);

			if (response.IsSuccessful)
			{
				return response.Data;
			}

			return DiscordUserStatus.New;
		}

		public async Task<bool> SetDiscordUserStatus(string discordUserId, DiscordUserStatus status)
		{
			var request = new RestRequest("api/DiscordUser/SetStatus", Method.GET);
			request.AddParameter("discordUserId", discordUserId);
			request.AddParameter("status", status);

			var response = await ExecuteGetTaskAsync(request);

			if (response.IsSuccessful)
			{
				return true;
			}

			return false;
		}
	}
}
