using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using Synthbot.Common;
using Synthbot.DAL.Models;

namespace Synthbot.DiscordBot.Services
{
	public class SpotifyInfoService
	{
		private readonly IConfiguration _config;
		private readonly SpotifyWebAPI _spotifyApi;
		public SpotifyInfoService(IConfiguration config, SpotifyWebAPI spotifyApi)
		{
			_config = config;
			_spotifyApi = spotifyApi;
		}

		public Uri GetLoginUriAuto(string userId, string username)
		{
			var jwt = JwtBuilder.BuildDiscordJwt(_config["synthbot.token.sharedsecret"], userId, username);

			var signinUri = new Uri($@"{_config["synthbot.webapp.protocol"]}://{_config["synthbot.webapp.host"]}/Identity/Account/ExternalLogin?provider=Spotify&referralToken={jwt}");

			return signinUri;
		}

		public Uri GetLoginUriManual(SocketCommandContext Context)
		{
			var jwt = JwtBuilder.BuildDiscordJwt(_config["synthbot.token.sharedsecret"], Context.User.Id.ToString(), Context.User.Username);

			var signinUri = new Uri($@"{_config["synthbot.webapp.protocol"]}://{_config["synthbot.webapp.host"]}/Identity/Account/ExternalLogin?provider=Spotify&referralToken={jwt}");

			return signinUri;
		}

		public async Task<IEnumerable<FullTrack>> GetNextTracksAsync(PlaybackSessionInfo session, string finishedSongUri = null, int previewCount = 2)
		{
			// Call spotifyApi to get playlist from playlistId
			// TODO this will have performance issues for larger playlists. Instead use the limit: and offset: params

			List<PlaylistTrack> playlistTracks;
				playlistTracks = (await _spotifyApi.GetPlaylistTracksAsync(null, session.SpotifyPlaylistId))
					.Items;

			// Get next track by finding the one after the finished song uri
			var finishedIndex = playlistTracks.FindIndex(t => t.Track.Uri == finishedSongUri);

			// Next song, or start back at 0
			var nextIndex = finishedIndex >= playlistTracks.Count - 1 ? 0 : finishedIndex + 1;

			// Take next track, plus preview tracks
			var requiredCount = 1 + previewCount;

			// Get the next song, and the previews
			var nextTracks = playlistTracks.Skip(nextIndex).Take(requiredCount).ToList();

			// If we are at the end of the playlist, get the previews from the start
			if (nextTracks.Count() < requiredCount)
			{
				var take = requiredCount - nextTracks.Count();
				nextTracks.AddRange(playlistTracks.Take(take));
			}

			return nextTracks.Select(t => t.Track);
		}

		public FullPlaylist GetFullPlaylist(string playlistId)
		{
			var playlist = _spotifyApi.GetPlaylist(null, playlistId);
			return playlist;
		}
	}
}
