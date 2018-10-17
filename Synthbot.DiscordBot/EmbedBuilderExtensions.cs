using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using SpotifyAPI.Web.Models;

namespace Synthbot.DiscordBot
{
	public static class EmbedBuilderExtensions
	{
		public static EmbedBuilder WithSynthbotAuthor(this EmbedBuilder builder, string discordVoiceChannelId)
		{
			return builder.WithAuthor(new EmbedAuthorBuilder()
				.WithName($"Synthbot - {discordVoiceChannelId}")
				.WithIconUrl("https://discordapp.com/assets/e05ead6e6ebc08df9291738d0aa6986d.png"));
		}

		public static EmbedBuilder WithAuthorFromContext(this EmbedBuilder builder, SocketCommandContext context) =>
			builder.WithAuthor(context.Client.CurrentUser);

		public static EmbedBuilder AddArtistField(this EmbedBuilder builder, List<SimpleArtist> artists)
		{
			var artistUrls = artists.Select(a => $"[{a.Name}]({a.ExternalUrls.First().Value})");

			var embedArtistName = new EmbedFieldBuilder()
				.WithName("Artist: ")
				.WithValue(string.Join(" & ", artistUrls))
				.WithIsInline(true);

			return builder.AddField(embedArtistName);
		}

		public static EmbedBuilder AddSongTitle(this EmbedBuilder builder, FullTrack track)
		{
			var embedSongTitleField = new EmbedFieldBuilder()
				.WithName("Song Title: ")
				.WithValue($"[{track.Name}]({track.ExternUrls.First().Value})")
				.WithIsInline(true);
			return builder.AddField(embedSongTitleField);
		}
	}
}
