using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web.Models;
using Synthbot.DAL.Models;
using Synthbot.DiscordBot.Services;
using Synthbot.WebApp.Client;

namespace Synthbot.DiscordBot
{
	/// <summary>
	/// Needs to be registered as scoped
	/// </summary>
	public class VoiceStatusChangedHandler
	{
		private readonly DiscordSocketClient _discord;
		private readonly SynthbotRestClient _synthbotRestClient;
		private readonly SpotifyInfoService _spotifyInfoService;
		private readonly IConfiguration _config;
		private readonly bool _notificationsEnabled;
		public VoiceStatusChangedHandler(
			DiscordSocketClient discord,
			SynthbotRestClient synthbotRestClient,
			SpotifyInfoService spotifyInfoService,
			IConfiguration config)
		{
			_discord = discord;
			_synthbotRestClient = synthbotRestClient;
			_spotifyInfoService = spotifyInfoService;
			_config = config;
			_notificationsEnabled = config.GetValue<bool>("NotificationsEnabled");

		}

		public async Task HandleJoined(SocketUser user, SocketVoiceChannel newChannel)
		{
			// Get user status
			var status = await GetDiscordUserStatus(user.Id);

			var synthbotUser = status != DiscordUserStatus.New || status != DiscordUserStatus.NoResponse
				? await _synthbotRestClient.GetUserFromDiscordIdAsync(user.Id.ToString())
				: null;

			var playlist = await GetPlaylist(newChannel.Name);

			switch (status)
			{
				case DiscordUserStatus.New when _notificationsEnabled:
				{
					var uri = _spotifyInfoService.GetLoginUriAuto(user.Id.ToString(), user.Username);
					await user.SendMessageAsync(
						$"Reply with \"@{_discord.CurrentUser.Username} notify\" to opt in to future notifications from me",
						false,
						EmbedFactory.Welcome.NewEmbed(user.Username, _discord.CurrentUser, uri));
					await _synthbotRestClient.SetDiscordUserStatus(user.Id.ToString(), DiscordUserStatus.NoResponse);
					return;
				}
				case DiscordUserStatus.RegisteredWithNotify when (synthbotUser?.AutoJoin ?? false):
				{
					await _synthbotRestClient.AddUserToChannel(newChannel.Name);
						if (_notificationsEnabled)
						{
							await user.SendMessageAsync("", false, EmbedFactory.Notifications.AutoJoinNotify(_discord.CurrentUser, newChannel, playlist));
						}
					return;
				}
				case DiscordUserStatus.RegisteredWithNotify when (!synthbotUser?.AutoJoin ?? false) && _notificationsEnabled:
					await user.SendMessageAsync("", false, EmbedFactory.Notifications.Notify(_discord.CurrentUser, newChannel, playlist));
					break;
				case DiscordUserStatus.RegisteredWithoutNotify when synthbotUser?.AutoJoin ?? false:
					await _synthbotRestClient.AddUserToChannel(newChannel.Name);
					return;
				case DiscordUserStatus.RegisteredWithoutNotify:
				case DiscordUserStatus.NoResponse:
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}
		}

		public async Task HandleMoved(SocketUser user, SocketVoiceChannel newChannel, SocketVoiceChannel oldChannel)
		{
			EmbedFieldBuilder previousPlaybackField = null;

			// Remove user from old channel's playback session
			await _synthbotRestClient.RemoveUserFromChannel(oldChannel.Name);

			// Get user status
			var status = await GetDiscordUserStatus(user.Id);

			if (status == DiscordUserStatus.RegisteredWithNotify)
			{
				var oldPlayback = await _synthbotRestClient.GetCurrentPlayback(oldChannel.Name);
				FullPlaylist oldPlaylist = null;
				if (oldPlayback != null)
				{
					oldPlaylist = _spotifyInfoService.GetFullPlaylist(oldPlayback.SpotifyPlaylistId);
				}
				if (oldPlaylist != null)
				{
					previousPlaybackField = EmbedFactory.Notifications.PreviousPlaybackField(oldChannel, oldPlaylist);
				}
			}

			var synthbotUser = await GetSynthbotUserIfExists(user.Id.ToString(), status);
			var playlist = await GetPlaylist(newChannel.Name);

			// Start playback on new channel if auto-join is on
			if (synthbotUser?.AutoJoin ?? false)
			{
				await _synthbotRestClient.AddUserToChannel(newChannel.Name);
				if (status == DiscordUserStatus.RegisteredWithNotify)
				{
					await user.SendMessageAsync("", false, EmbedFactory.Notifications.AutoJoinNotify(_discord.CurrentUser, newChannel, playlist, previousPlaybackField));
				}
			}
			else
			{
				if (status == DiscordUserStatus.RegisteredWithNotify)
				{
					await user.SendMessageAsync("", false, EmbedFactory.Notifications.Notify(_discord.CurrentUser, newChannel, playlist, previousPlaybackField));
				}
			}
		}

		public async Task HandleExit(SocketUser user, SocketVoiceChannel oldChannel)
		{
			// Get user status
			var status = await GetDiscordUserStatus(user.Id);

			// Remove user from old channel's playback session
			await _synthbotRestClient.RemoveUserFromChannel(oldChannel.Name);
			if (status == DiscordUserStatus.RegisteredWithNotify && _notificationsEnabled)
			{
				await user.SendMessageAsync($"Playback stopped in {oldChannel.Name}");
			}
			return;
		}

		private async Task<SynthbotUser> GetSynthbotUserIfExists(string discordUserId, DiscordUserStatus status)
		{
			return status != DiscordUserStatus.New || status != DiscordUserStatus.NoResponse
				? await _synthbotRestClient.GetUserFromDiscordIdAsync(discordUserId)
				: null;
		}

		private async Task<FullPlaylist> GetPlaylist(string newChannelName)
		{
			var playback = await _synthbotRestClient.GetCurrentPlayback(newChannelName);
			var playlist = playback != null
				? _spotifyInfoService.GetFullPlaylist(playback.SpotifyPlaylistId)
				: null;
			return playlist;
		}

		private async Task<DiscordUserStatus> GetDiscordUserStatus(ulong discordUserId)
		{
			return await _synthbotRestClient.GetDiscordUserStatus(discordUserId.ToString());
		}
	}
}
