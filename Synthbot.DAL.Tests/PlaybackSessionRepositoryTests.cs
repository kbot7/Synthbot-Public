using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.DAL.Tests.Helpers;
using Xunit;

namespace Synthbot.DAL.Tests
{
	public class PlaybackSessionRepositoryTests
	{
		[Fact]
		public async Task GetResponse_Null()
		{
			using (var context = await EntityFrameworkHelpers.SqlContextAsync())
			{
				var songRepo = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);
				var sessionRepo = new PlaybackSessionRepository(new NullLogger<PlaybackSessionRepository>(), context, songRepo);

				var result = await sessionRepo.GetById("foo");

				Assert.Null(result);
			}
		}

		[Fact]
		public async Task GetResponse_Valid()
		{
			using (var context = await EntityFrameworkHelpers.SqlContextAsync())
			{
				var original = new PlaybackSession()
				{
					Id = "foo",
					SpotifyPlaylistId = "spotify-playlist",
					DiscordVoiceChannelId = "discord-channel-id"
				};

				await context.PlaybackSessions.AddAsync(original);

				await context.SaveChangesAsync();

				var songRepo = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);
				var sessionRepo = new PlaybackSessionRepository(new NullLogger<PlaybackSessionRepository>(), context, songRepo);

				var result = await sessionRepo.GetById("foo");

				Assert.NotNull(result);
				Assert.Equal(result, original);
			}
		}

		[Fact]
		public async Task Upsert_New()
		{
			using (var context = await EntityFrameworkHelpers.SqlContextAsync())
			{
				var original = new PlaybackSession()
				{
					Id = "foo",
					SpotifyPlaylistId = "spotify-playlist",
					DiscordVoiceChannelId = "discord-channel-id"
				};

				var songRepo = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);
				var sessionRepo = new PlaybackSessionRepository(new NullLogger<PlaybackSessionRepository>(), context, songRepo);

				await sessionRepo.UpsertSession(original);

				var result = await sessionRepo.GetById("foo");

				Assert.NotNull(result);
				Assert.Equal(result, original);
			}
		}

		[Fact]
		public async Task Upsert_New_WithCurrentPlayback()
		{
			using (var context = await EntityFrameworkHelpers.SqlContextAsync())
			{
				var originalSongTracker = new SongPlaybackTracker()
				{
					Id = "foo-song",
					SpotifySongUri = "test-spotify-song-uri",
					State = PlaybackState.Playing,
					Duration = TimeSpan.FromSeconds(30),
					StartedUtc = DateTime.UtcNow
				};

				var original = new PlaybackSession()
				{
					Id = "foo",
					SpotifyPlaylistId = "spotify-playlist",
					DiscordVoiceChannelId = "discord-channel-id",
					CurrentSongPlayback = originalSongTracker
				};

				var songRepo = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);
				var sessionRepo = new PlaybackSessionRepository(new NullLogger<PlaybackSessionRepository>(), context, songRepo);

				await sessionRepo.UpsertSession(original);

				var result = await sessionRepo.GetById("foo");

				var songResult = await songRepo.GetById("foo-song");

				Assert.NotNull(result);
				Assert.Equal(result, original);
				Assert.NotNull(result.CurrentSongPlayback);
				Assert.Equal(result.CurrentSongPlayback, originalSongTracker);
				Assert.Equal(result, songResult.PlaybackSession);
			}
		}
	}
}
