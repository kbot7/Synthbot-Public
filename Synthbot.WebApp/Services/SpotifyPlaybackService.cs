using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace Synthbot.WebApp.Services
{
	public class SpotifyPlaybackService
	{
		private readonly SpotifyHttpClientFactory _spotifyApiFactory;
		private readonly UserService _userService;
		public SpotifyPlaybackService(
			SpotifyHttpClientFactory spotifyApiFactory,
			UserService userService)
		{
			_spotifyApiFactory = spotifyApiFactory;
			_userService = userService;
		}

		public async Task<bool> StartSpotifyUserPlaybackAsync(IEnumerable<string> synthbotUserIds, string spotifySongUri,
			int startMs = 0)
		{

			var startPlaybackTasks = synthbotUserIds
				.Select(id => StartSpotifyUserPlaybackAsync(id, spotifySongUri, startMs));
			await Task.WhenAll(startPlaybackTasks);

			return true;
		}

		public async Task<bool> StartSpotifyUserPlaybackAsync(string synthbotUserId, string spotifySongUri, int startMs = 0)
		{
			SpotifyWebAPI spotifyClient = null;
			try
			{
				// Get a spotify client for the user
				spotifyClient = await _spotifyApiFactory.CreateUserClientAsync(synthbotUserId);
				return await StartSpotifyUserPlaybackAsync(spotifyClient, synthbotUserId, spotifySongUri, startMs);
			}
			finally
			{
				spotifyClient?.Dispose();
			}
		}

		public async Task<bool> StartSpotifyUserPlaybackAsync(SpotifyWebAPI spotifyClient, string synthbotUserId,
			string spotifySongUri, int startMs = 0)
		{
			// Get user's default device or current active device
			var user = await _userService.GetUserBySynthbotUserIdAsync(synthbotUserId);
			string deviceId = null;
			if (string.IsNullOrWhiteSpace(user.DefaultSpotifyDevice))
			{
				var playback = await spotifyClient.GetPlaybackAsync();
				deviceId = playback?.Device?.Id;

				if (!string.IsNullOrWhiteSpace(deviceId))
				{
					// TODO set this during an intial setup, instead of here
					await _userService.SetDefaultDevice(user, deviceId);
				}
			}
			else
			{
				deviceId = user.DefaultSpotifyDevice;
			}

			// If there is a device available, send spotify client command
			if (!string.IsNullOrWhiteSpace(deviceId))
			{
				var resumeResponse = await spotifyClient.ResumePlaybackAsync(
					deviceId: deviceId,
					uris: new List<string>() { spotifySongUri },
					offset: 0,
					positionMs: startMs);

				return true;
			}
			return false;
		}

		public async Task<bool> PauseSpotifyUserPlaybackAsync(string synthbotUserId)
		{
			SpotifyWebAPI spotifyClient = null;
			try
			{
				spotifyClient = await _spotifyApiFactory.CreateUserClientAsync(synthbotUserId);

				// Get Active Device
				// TODO get this from a user-configurable option instead. Perhaps part of the registration flow in the discord bot. If we do that, the discord bot needs access to user spotify keys.
				var playback = await spotifyClient.GetPlaybackAsync();
				var activeDeviceId = playback.Device.Id;

				await spotifyClient.PausePlaybackAsync(activeDeviceId);
			}
			finally
			{
				spotifyClient?.Dispose();
			}

			return true;
		}

		public async Task<bool> PauseSpotifyUserPlaybackAsync(IEnumerable<string> synthbotUserId)
		{
			bool result = true;
			// TODO investigate doing this in parallel. Will need better thread safety regarding the access token before, and I'll need to get rid of the semaphore
			foreach (var id in synthbotUserId)
			{
				result = result && await PauseSpotifyUserPlaybackAsync(id);
			}
			return result;
		}
	}
}
