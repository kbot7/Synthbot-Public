using System;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SpotifyAPI.Web.Models;
using Synthbot.DAL.Models;

namespace Synthbot.DiscordBot
{
	public static class EmbedFactory
	{
		public static Embed NowPlaying(
			string discordVoiceChannelId,
			FullTrack startingTrack,
			FullTrack nextTrack,
			FullPlaylist currentPlaylist)
		{
			
			var duration = TimeSpan.FromMilliseconds(startingTrack.DurationMs);
			var artwork = startingTrack.Album.Images.First();
			var embedFooter = new EmbedFooterBuilder()
				.WithText($"Next up: {string.Join(" & ", nextTrack.Artists.Select(a => a.Name))} - {nextTrack.Name}")
				.WithIconUrl("https://discordapp.com/assets/28174a34e77bb5e5310ced9f95cb480b.png");
			var embedAlbumField = new EmbedFieldBuilder()
				.WithName("Album: ")
				.WithValue($"[{startingTrack.Album.Name}]({startingTrack.Album.ExternalUrls.First().Value})")
				.WithIsInline(true);
			var embedTimeField = new EmbedFieldBuilder()
				.WithName("Time: ")
				.WithValue($"{duration:mm\\:ss}")
				.WithIsInline(true);

			var embed = new EmbedBuilder()
				.WithColor(Color.Blue)
				.WithSynthbotAuthor(discordVoiceChannelId)
				.AddSongTitle(startingTrack)
				.AddArtistField(startingTrack.Artists)
				.AddField(embedAlbumField)
				.AddField(embedTimeField)
				.WithThumbnailUrl(artwork.Url)
				.WithFooter(embedFooter)
				.WithCurrentTimestamp();
			if (currentPlaylist == null)
			{
				return embed.Build();
			}

			var embedPlaylistField = new EmbedFieldBuilder()
				.WithName("Current Playlist: ")
				.WithValue($"[{currentPlaylist.Name}]({currentPlaylist.ExternalUrls.First().Value})")
				.WithIsInline(true);
			var embedPlaylistOwnerField = new EmbedFieldBuilder()
				.WithName("Playlist Owner: ")
				.WithValue($"[{currentPlaylist.Owner.Id}]({currentPlaylist.Owner.ExternalUrls.First().Value})")
				.WithIsInline(true);
			embed.AddField(embedPlaylistField);
			embed.AddField(embedPlaylistOwnerField);
			return embed.Build();
		}

		public static class Notifications
		{
			public static EmbedFieldBuilder PreviousPlaybackField(SocketVoiceChannel oldChannel, FullPlaylist previousPlaylist)
			{
				var embedField = new EmbedFieldBuilder()
					.WithName($"Previous Playback Channel: {oldChannel.Name}")
					.WithValue($"Active Playlist: [{previousPlaylist.Name}]({previousPlaylist.ExternalUrls.First().Value}) From: [{previousPlaylist.Owner.Id}]({previousPlaylist.Owner.ExternalUrls.First().Value})")
					.WithIsInline(false);
				return embedField;
			}

			public static Embed Notify(SocketSelfUser bot, SocketVoiceChannel newChannel, FullPlaylist newPlaylist, EmbedFieldBuilder previousPlaybackField = null)
			{
				var embedNewPlayback = new EmbedFieldBuilder()
					.WithName($"Joined Channel: {newChannel.Name}\t")
					.WithValue($"Active Playlist: [{newPlaylist.Name}]({newPlaylist.ExternalUrls.First().Value}) From: [{newPlaylist.Owner.Id}]({newPlaylist.Owner.ExternalUrls.First().Value})\t")
					.WithIsInline(false);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthor(bot)
					.WithTitle($"There's an active playback session in {newChannel.Name}, run \"@{bot.Username} join\" to start listening")
					.WithCurrentTimestamp();
				if (previousPlaybackField != null)
				{
					embed.AddField(previousPlaybackField);
				}
				embed.AddField(embedNewPlayback);
				return embed.Build();
			}

			public static Embed NotifyState(SocketCommandContext context, DiscordUserStatus status)
			{
				var formattedStatus = status == DiscordUserStatus.RegisteredWithNotify
					? $"Notifications are currently on for {context.User.Username}"
					: $"Notifications are currently off for {context.User.Username}";
				var currentStatus = new EmbedFieldBuilder()
					.WithName(formattedStatus)
					.WithValue("Would you like to change your notification status? (Reply with yes or no)")
					.WithIsInline(false);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthor(context.Client.CurrentUser)
					.AddField(currentStatus);
				return embed.Build();
			}

			public static Embed NotifyUpdate(SocketCommandContext context, DiscordUserStatus status, string reply)
			{
				var embed = new EmbedBuilder()
					.WithAuthorFromContext(context);
				switch (reply.ToLower())
				{
					case "yes":
					{
						var updatedField = new EmbedFieldBuilder()
							.WithName($"Notification status for {context.User.Username} has been updated.")
							.WithValue("You are now receiving DM notifications from us.")
							.WithIsInline(false);
						embed.AddField(updatedField);
						embed.WithColor(Color.Green);
						break;
					}
					case "no":
					{
						var updatedField = new EmbedFieldBuilder()
							.WithName($"Notification status for {context.User.Username} has not been updated.")
							.WithValue("YYou will no longer receive DM notifications from us.")
							.WithIsInline(false);
						embed.AddField(updatedField);
						embed.WithColor(Color.Green);
						break;
					}
					default:
					{
						var updatedField = new EmbedFieldBuilder()
							.WithName($"Notification status for {context.User.Username} is unchanged.")
							.WithValue("Invalid response received.")
							.WithIsInline(false);
						embed.AddField(updatedField);
						embed.WithColor(Color.Red);
						break;
					}
				}
				return embed.Build();
			}

			public static Embed AutoJoinNotify(SocketSelfUser bot, SocketVoiceChannel newChannel, FullPlaylist newPlaylist, EmbedFieldBuilder previousPlaybackField = null)
			{
				var embedNewPlayback = new EmbedFieldBuilder()
					.WithName($"Joined Channel: {newChannel.Name}")
					.WithValue($"Active Playlist: [{newPlaylist.Name}]({newPlaylist.ExternalUrls.First().Value}) From: [{newPlaylist.Owner.Id}]({newPlaylist.Owner.ExternalUrls.First().Value})")
					.WithIsInline(false);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthor(bot)
					.WithTitle($"Auto-Join just joined you to an active playback session in {newChannel.Name}")
					.WithCurrentTimestamp();
				if (previousPlaybackField != null)
				{
					embed.AddField(previousPlaybackField);
				}
				embed.AddField(embedNewPlayback);
				return embed.Build();
			}
		}

		public static class Welcome
		{
			public static Embed NewEmbed(string username, SocketSelfUser synthbot, Uri signinUri)
			{
				var linkEmbed = new EmbedFieldBuilder()
					.WithName($"Click the link below to connect your Spotify account and run \"@{synthbot.Username} join\" to start listening!")
					.WithValue(signinUri.AbsoluteUri);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthor(synthbot)
					.WithTitle($"Hey {username}, you just joined a voice channel with an active Synthbot channel")
					.AddField(linkEmbed)
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Login
		{
			public static Embed DmLink(SocketCommandContext context, Uri signinUri)
			{
				var linkField = new EmbedFieldBuilder()
					.WithName("Click the link below to connect your Spotify account")
					.WithValue(signinUri.AbsoluteUri);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthorFromContext(context)
					.WithTitle($"Welcome {context.User.Username}!")
					.AddField(linkField)
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Join
		{
			public static Embed JoinSuccess(SocketCommandContext context, string channelName, string deviceName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} has joined playback in {channelName}")
					.WithDescription($"Starting playback on: {deviceName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}

			public static Embed JoinFailed(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} has failed joining playback in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Leave
		{
			public static Embed LeaveTrue(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.LightOrange)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} has left playback in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}

			public static Embed LeaveFailed(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.LightOrange)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} has failed to leave playback in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Pause
		{
			public static Embed PauseTrue(FullTrack track, TimeSpan pausedAt, SocketCommandContext context, string channelName)
			{
				var embedSongName = new EmbedFieldBuilder()
					.WithName("Song Title: ")
					.WithValue($"[{track.Name}]({track.ExternUrls.First().Value})")
					.WithIsInline(true);
				var embedAlbumName = new EmbedFieldBuilder()
					.WithName("Album: ")
					.WithValue($"[{track.Album.Name}]({track.Album.ExternalUrls.First().Value})")
					.WithIsInline(true);
				var embedPausedAt = new EmbedFieldBuilder()
					.WithName("Paused at: ")
					.WithValue($"{pausedAt:mm\\:ss}/{TimeSpan.FromMilliseconds(track.DurationMs):mm\\:ss}")
					.WithIsInline(true);
				var embed = new EmbedBuilder()
					.WithColor(Color.LightOrange)
					.WithAuthorFromContext(context)
					.WithThumbnailUrl(track.Album.Images.First().Url)
					.WithTitle($"{context.User.Username} has paused playback in {channelName}")
					.AddField(embedSongName)
					.AddArtistField(track.Artists)
					.AddField(embedAlbumName)
					.AddField(embedPausedAt)
					.WithCurrentTimestamp();
				return embed.Build();
			}

			public static Embed PauseFailed(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} failed to pause playback in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Resume
		{
			public static Embed ResumeTrue(FullTrack track, TimeSpan pausedAt, SocketCommandContext context, string channelName)
			{
				
				var embedSongName = new EmbedFieldBuilder()
					.WithName("Song Title: ")
					.WithValue($"[{track.Name}]({track.ExternUrls.First().Value})")
					.WithIsInline(true);
				var embedAlbumName = new EmbedFieldBuilder()
					.WithName("Album: ")
					.WithValue($"[{track.Album.Name}]({track.Album.ExternalUrls.First().Value})")
					.WithIsInline(true);
				var embedPausedAt = new EmbedFieldBuilder()
					.WithName("Resuming from: ")
					.WithValue($"{pausedAt:mm\\:ss}/{TimeSpan.FromMilliseconds(track.DurationMs):mm\\:ss}")
					.WithIsInline(true);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthorFromContext(context)
					.WithThumbnailUrl(track.Album.Images.First().Url)
					.WithTitle($"{context.User.Username} has resumed playback in {channelName}")
					.AddField(embedSongName)
					.AddArtistField(track.Artists)
					.AddField(embedAlbumName)
					.AddField(embedPausedAt)
					.WithCurrentTimestamp();
				return embed.Build();
			}

			public static Embed ResumeFailed(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} failed to resume playback in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Skip
		{
			public static Embed SkipTrue(FullTrack track, TimeSpan skippedAt, SocketCommandContext context, string channelName)
			{
				
				var embedAlbumField = new EmbedFieldBuilder()
					.WithName("Album: ")
					.WithValue($"[{track.Album.Name}]({track.Album.ExternalUrls.First().Value})")
					.WithIsInline(true);
				var embedSkippedAt = new EmbedFieldBuilder()
					.WithName("Skipped at: ")
					.WithValue($"{skippedAt:mm\\:ss}/{TimeSpan.FromMilliseconds(track.DurationMs):mm\\:ss}")
					.WithIsInline(true);
				var embed = new EmbedBuilder()
					.WithColor(Color.Gold)
					.WithAuthorFromContext(context)
					.WithThumbnailUrl(track.Album.Images.First().Url)
					.WithTitle($"{context.User.Username} has skipped playback in {channelName}")
					.AddSongTitle(track)
					.AddArtistField(track.Artists)
					.AddField(embedAlbumField)
					.AddField(embedSkippedAt)
					.WithCurrentTimestamp();
				return embed.Build();
			}

			public static Embed SkipFalse(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} failed to skip playback in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}


			
		}
		

		public static class ChangePlaylist
		{
			public static Embed ChangePlaylistTrue(SocketCommandContext context, string channelName, FullPlaylist newPlaylist, FullPlaylist previousPlaylist = null)
			{
				var embedNewField = new EmbedFieldBuilder()
					.WithName("New Playlist: ")
					.WithValue($"[{newPlaylist.Name}]({newPlaylist.ExternalUrls.First().Value})")
					.WithIsInline(true);
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} has changed the playlist in {channelName}")
					.AddField(embedNewField)
					.WithCurrentTimestamp();
				if (previousPlaylist == null)
				{
					return embed.Build();
				}

				var embedPreviousField = new EmbedFieldBuilder()
					.WithName("Previous Playlist: ")
					.WithValue($"[{previousPlaylist.Name}]({previousPlaylist.ExternalUrls.First().Value})")
					.WithIsInline(true);
				embed.AddField(embedPreviousField);
				return embed.Build();
			}

			public static Embed ChangePlaylistFailed(SocketCommandContext context, string channelName)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} failed to change the playlist in {channelName}")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class AutoJoin
		{
			public static Embed AutoJoinTrue(SocketCommandContext context, bool autoJoin)
			{
				var autojoinState = new EmbedFieldBuilder()
					.WithName($"Auto-Join for {context.User.Username} has been set to: ")
					.WithValue(autoJoin.ToString());
				var embed = new EmbedBuilder()
					.WithColor(Color.Green)
					.WithAuthorFromContext(context)
					.AddField(autojoinState)
					.WithCurrentTimestamp();
				return embed.Build();
			}

			public static Embed AutoJoinFalse(SocketCommandContext context)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"Failed to update Auto-Join for {context.User.Username}")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class SetUpdateChannel
		{
			public static Embed GroupChannelNull(SocketCommandContext context)
			{
				var embed = new EmbedBuilder()
					.WithColor(Color.Red)
					.WithAuthorFromContext(context)
					.WithTitle($"{context.User.Username} you can only run that command from a text channel")
					.WithCurrentTimestamp();
				return embed.Build();
			}
		}

		public static class Reset
		{

		}

		public static class Devices
		{

		}

		public static class SetDevice
		{

		}
	}
}
