using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Synthbot.Common;
using Synthbot.DAL.Models;
using Synthbot.DiscordBot.Services;
using Synthbot.WebApp.Client;

namespace Synthbot.DiscordBot.Modules
{
	public class SpotifyModule : ModuleBase<SocketCommandContext>
	{
		private readonly IConfiguration _config;
		private readonly SpotifyWebAPI _spotifyApi;
		private readonly SynthbotRestClient _synthbotWebClient;
		private readonly SpotifyInfoService _spotifyInfoService;
		private readonly ILogger<SpotifyModule> _logger;
		public SpotifyModule(
			IConfiguration config,
			SynthbotRestClient synthbotWebClient,
			SpotifyInfoService spotifyInfoService,
			ILogger<SpotifyModule> logger,
			SpotifyWebAPI spotifyApi)
		{
			_config = config;
			_synthbotWebClient = synthbotWebClient;
			_spotifyInfoService = spotifyInfoService;
			_logger = logger;
			_spotifyApi = spotifyApi;
		}

		[Command("login", RunMode = RunMode.Async)]
		[Summary("Login to Synthbot")]
		public async Task SignInSpotify()
		{
			var signinUri = _spotifyInfoService.GetLoginUriManual(Context);
			var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
			await dmChannel.SendMessageAsync("", false, EmbedFactory.Login.DmLink(Context, signinUri));
			await ReplyAsync($"{Context.User.Mention} Check your DMs for a link to verify your Spotify account.");
		}

		[Command("join", RunMode = RunMode.Async)]
		[Summary("Join playback on your connected voice channel. You must have Spotify open")]
		public async Task JoinChannel(string overrideChannelName = null)
		{
			// Get the the name of the user's joined voicechannel and send an error message if not joined
			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			if (string.IsNullOrWhiteSpace(channelName)) return;

			// Gets the synthbot user and sends an error message if their default device isn't set
			var synthbotUser = await _synthbotWebClient.GetUserFromDiscordIdAsync(Context.User.Id.ToString());
			if (string.IsNullOrWhiteSpace(synthbotUser.DefaultSpotifyDevice))
			{
				await ReplyAsync($"{Context.User.Mention} you have no playback device set, run \"{Context.Client.CurrentUser.Mention} set-device\" to set one.");
				return;
			}

			// Gets a list of user's available Spotify devices
			var token = await _synthbotWebClient.GetSpotifyToken();

			_spotifyApi.AccessToken = token;

			var response = await _spotifyApi.GetDevicesAsync();
			if (response.HasError())
			{
				await ReplyAsync($"Error | Status: {response.Error.Status} Message: {response.Error.Message}");
				return;
			}

			var devices = response?.Devices;
			if (devices == null || !devices.Any())
			{

				await ReplyAsync($"{Context.User.Mention} Unable to find any Spotify devices for your account");
				return;
			}

			// Check if the user's default device is in their list of devices. If their device isn't open, it wont be in the list, so we alert the user
			var currentDevice = devices.FirstOrDefault(a => a.Id == synthbotUser.DefaultSpotifyDevice);
			if (currentDevice == null)
			{
				await ReplyAsync($"{Context.User.Mention} your selected Spotify player is not online. Make sure it is open before running this command");
				return;
			}

			// Tries to add user to playback and sends an error message if unable to
			var result = await _synthbotWebClient.AddUserToChannel(channelName);

			if (result == true)
			{
				await ReplyAsync("", false, EmbedFactory.Join.JoinSuccess(Context, channelName, currentDevice.Name));
			}
			else
			{
				await ReplyAsync("", false, EmbedFactory.Join.JoinFailed(Context, channelName));
			}


		}

		[Command("leave", RunMode = RunMode.Async)]
		[Summary("Leave the playback session, and pause playback")]
		public async Task LeaveChannel(string overrideChannelName = null)
		{
			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			if (string.IsNullOrWhiteSpace(channelName)) return;

			var result = await _synthbotWebClient.RemoveUserFromChannel(channelName);
			if (result == true)
			{
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Leave.LeaveTrue(Context, channelName));
			}
			else
			{
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Leave.LeaveFailed(Context, channelName));
			}
		}

		[Command("spotify-token", RunMode = RunMode.Async)]
		[Summary("DEV USE ONLY: Have a fresh Spotify access token DMed to you")]
		public async Task GetToken()
		{
			var result = await _synthbotWebClient.GetSpotifyToken();

			var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
			await dmChannel.SendMessageAsync($"Spotify Access Token: {result}");
		}

		[Command("pause", RunMode = RunMode.Async)]
		[Summary("Pause playback on your connected voice channel")]
		public async Task PauseChannel(string overrideChannelName = null)
		{
			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			if (string.IsNullOrWhiteSpace(channelName)) return;

			var result = await _synthbotWebClient.PausePlayback(channelName);
			var ms = Convert.ToDouble(result.PausedMs);
			var pausedAt = TimeSpan.FromMilliseconds(ms);
			if (result != null)
			{
				var track = await _spotifyApi.GetTrackAsync(result.TrackId);
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Pause.PauseTrue(track, pausedAt, Context, channelName));
			}
			else
			{
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Pause.PauseFailed(Context, channelName));
			}
		}

		[Command("resume", RunMode = RunMode.Async)]
		[Summary("Resume playback on your connected current voice channel")]
		public async Task ResumeChannel(string overrideChannelName = null)
		{
			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			if (string.IsNullOrWhiteSpace(channelName)) return;

			var result = await _synthbotWebClient.ResumePlayback(channelName);
			var ms = Convert.ToDouble(result.PausedMs);
			var pausedAt = TimeSpan.FromMilliseconds(ms);
			if (result != null)
			{
				var track = await _spotifyApi.GetTrackAsync(result.TrackId);
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Resume.ResumeTrue(track, pausedAt, Context, channelName));
			}
			else
			{
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Resume.ResumeFailed(Context, channelName));
			}
		}

		[Command("skip", RunMode = RunMode.Async)]
		[Summary("Skip the current song on your connected voice channel")]
		public async Task Skip(string overrideChannelName = null)
		{
			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			if (string.IsNullOrWhiteSpace(channelName)) return;

			var result = await _synthbotWebClient.Skip(channelName);
			if (result != null)
			{
				var track = await _spotifyApi.GetTrackAsync(result.CurrentSongPlaybackId);
				var SkippedAt = DateTime.Now.Subtract(result.StartedUtc);
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Skip.SkipTrue(track, SkippedAt, Context, channelName));
			}
			else
			{
				await Context.Channel.SendMessageAsync("", false, EmbedFactory.Skip.SkipFalse(Context, channelName));
			}
		}

		[Command("change-playlist", RunMode = RunMode.Async)]
		[Summary("Change the playlist of your current voice channel")]
		public async Task ChangePlaylist(
			[Name("New Playlist Link")]
			[Summary("Can be a link (url) to the playlist, a spotify playlist uri, or a playlist id")]
			string newPlaylistString,
			[Remainder] string overrideChannelName = null)
		{
			string playlistId = null;
			// Is SpotifyUri
			if (newPlaylistString.StartsWith("spotify:"))
			{
				// spotify:user:126657900:playlist:4APahj7sAvW27bj0DUhHWA
				var parts = newPlaylistString.Split(':');
				var playlistNameIndex = parts.IndexOf("playlist");
				var playlistValueIndex = playlistNameIndex + 1;
				if (playlistNameIndex > -1 && playlistValueIndex <= parts.Length)
				{
					playlistId = parts[playlistValueIndex];
				}
				else
				{
					await ReplyAsync($"The entered playlist playlist uri was not recognized.");
					return;
				}
			}
			// Is link
			else if (newPlaylistString.StartsWith("https://open.spotify.com"))
			{
				// https://open.spotify.com/user/126657900/playlist/4APahj7sAvW27bj0DUhHWA?si=Xa1OQKqMRDmEd_U1Dq3cCg
				var parts = newPlaylistString.Split('/');
				var playlistNameIndex = parts.IndexOf("playlist");
				var playlistValueIndex = parts.IndexOf("playlist") + 1;
				if (playlistNameIndex > -1 && playlistValueIndex <= parts.Length)
				{
					var playlistPart = parts[playlistValueIndex];

					var queryIndex = playlistPart.IndexOf('?');

					playlistId = queryIndex == -1
						? playlistPart
						: playlistPart.Substring(0, queryIndex);
				}
				else
				{
					await ReplyAsync($"The entered playlist url was not recognized.");
					return;
				}
			}
			// Is (hopefully) the bare id
			else
			{
				// Validate playlist Id
				var playlist = await _spotifyApi.GetPlaylistAsync(null, newPlaylistString);
				if (playlist.HasError())
				{
					await ReplyAsync($"The entered playlist ID was not recognized. Error Message: {playlist.Error.Message}");
					return;
				}
				else
				{
					playlistId = newPlaylistString.Trim();
				}
			}

			// If we couldn't parse the playlist, return a message
			if (string.IsNullOrWhiteSpace(playlistId))
			{
				await ReplyAsync("The entered playlist was not recognized");
				return;
			}

			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			var result = await _synthbotWebClient.ChangePlaylist(channelName, playlistId);
			if (result == null)
			{
				await ReplyAsync("", false, EmbedFactory.ChangePlaylist.ChangePlaylistFailed(Context, channelName));
			}
			var newPlaylist = await _spotifyApi.GetPlaylistAsync(null, result.NewPlaylist);
			if (string.IsNullOrWhiteSpace(result.PreviousPlaylist))
			{
				await ReplyAsync("", false, EmbedFactory.ChangePlaylist.ChangePlaylistTrue(Context, channelName, newPlaylist));
				return;
			}
			var previousPlaylist = await _spotifyApi.GetPlaylistAsync(null, result.PreviousPlaylist);
			await ReplyAsync("", false, EmbedFactory.ChangePlaylist.ChangePlaylistTrue(Context, channelName, newPlaylist, previousPlaylist));
		}

		[Command("auto-join", RunMode = RunMode.Async)]
		[Summary("Use `auto-join true` to automatically join playback when connecting to voice channels")]
		public async Task AutoJoin(
			[Name("true/false")]
			[Summary("Set to true to automatically join synthbot when joining a voice channel")]
			bool autoJoin)
		{
			var result = await _synthbotWebClient.SetAutoJoin(Context.User.Id.ToString(), autoJoin);
			if (result == false)
			{
				await ReplyAsync("", false, EmbedFactory.AutoJoin.AutoJoinFalse(Context));
				return;
			}
			await ReplyAsync("", false, EmbedFactory.AutoJoin.AutoJoinTrue(Context, autoJoin));
		}

		[Command("set-update-channel", RunMode = RunMode.Async)]
		[Summary("Set the text channel Synthbot will post updates to. Run this command in the text channel you would like updates posted in")]
		public async Task UpdateChannel(string overrideChannelName = null)
		{
			var channelName = await GetUserVoiceChannelName(overrideChannelName);
			#pragma warning disable IDE0019 // Use pattern matching
			var textChannel = Context.Channel as ITextChannel;
			#pragma warning restore IDE0019 // Use pattern matching
			if (textChannel == null)
			{
				await ReplyAsync("Can only be run from a text channel");
				return;
			}

			var channelId = Context.Channel.Id;

			var result = await _synthbotWebClient.SetTextUpdateChannel(channelName, textChannel.Id.ToString());

			await ReplyAsync($"Success: {result}");
		}

		[Command("reset", RunMode = RunMode.Async)]
		[Summary("DEV USE ONLY: Clears the database and all jobs. Unjoins all users")]
		public async Task Reset()
		{
			var result = await _synthbotWebClient.Reset();

			await ReplyAsync($"Success: {result}");
		}

		[Command("get-my-status", RunMode = RunMode.Async)]
		[Summary("DEV USE ONLY: View discord user status (registered, ignore, etc)")]
		public async Task GetUserStatus()
		{
			var userId = Context.User.Id.ToString();
			var status = await _synthbotWebClient.GetDiscordUserStatus(userId);
			await ReplyAsync($"Status: {status.GetDescription()}");
		}

		[Command("set-my-status", RunMode = RunMode.Async)]
		[Summary("DEV USE ONLY: Set your user's status")]
		public async Task SetUserStatus(
			[Summary("Possible statuses: NoResponse, IgnoreForever, RemindMeLater, Registered, New")]
			DiscordUserStatus status)
		{
			var response = await _synthbotWebClient.SetDiscordUserStatus(Context.User.Id.ToString(), status);
			await ReplyAsync($"Success: {response}");
		}

		private async Task<string> GetUserVoiceChannelName(string overrideChannelName = null)
		{
			var guildUser = Context.User as SocketGuildUser;
			var voiceChannelName = guildUser?.VoiceChannel?.Name;

			if (!string.IsNullOrWhiteSpace(overrideChannelName))
			{
				return overrideChannelName;
			}

			if (string.IsNullOrWhiteSpace(voiceChannelName))
			{
				await ReplyAsync("You must be joined to a voice channel to make this command, or you must specify a channel name at the end of your command");
			}
			return voiceChannelName;
		}
	}
}
