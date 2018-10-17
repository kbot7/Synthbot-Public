using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.DAL.Tests.Helpers;
using Xunit;

namespace Synthbot.DAL.Tests
{
	public class SongPlaybackRepositoryTests
	{
		[Fact]
		public async Task GetResponse_Null()
		{
			using (var context = new ApplicationDbContext(EntityFrameworkHelpers.InMemoryOptions()))
			{
				var service = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);

				var result = await service.GetById("foo");

				Assert.Null(result);
			}
		}

		[Fact]
		public async Task GetResponse_Valid()
		{
			using (var context = new ApplicationDbContext(EntityFrameworkHelpers.SqlDb()))
			{
				await context.Database.EnsureDeletedAsync();
				await context.Database.EnsureCreatedAsync();
				await context.Database.MigrateAsync();
				var record = new SongPlaybackTracker()
				{
					Id = "foo",
					SpotifySongUri = "test-spotify-song-uri",
					State = PlaybackState.Playing,
					Duration = TimeSpan.FromSeconds(30),
					StartedUtc = DateTime.UtcNow,
					PlaybackSession = null,
					PlaybackSessionId = null
				};

				await context.SongPlaybackTrackers.AddAsync(record);

				await context.SaveChangesAsync();

				var service = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);

				var result = await service.GetById("foo");

				Assert.NotNull(result);
				Assert.Equal(result, record);

				await context.Database.EnsureDeletedAsync();
			}
		}

		[Fact]
		public async Task Upsert_New()
		{
			using (var context = new ApplicationDbContext(EntityFrameworkHelpers.SqlDb()))
			{
				await context.Database.EnsureDeletedAsync();
				await context.Database.EnsureCreatedAsync();
				await context.Database.MigrateAsync();

				try
				{
					var record = new SongPlaybackTracker()
					{
						Id = "foo",
						SpotifySongUri = "test-spotify-song-uri",
						State = PlaybackState.Playing,
						Duration = TimeSpan.FromSeconds(30),
						StartedUtc = DateTime.UtcNow,
						PlaybackSession = null,
						PlaybackSessionId = null
					};

					var service = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);

					await service.Upsert(record);

					var response = await service.GetById("foo");

					var result = await service.GetById("foo");

					Assert.NotNull(result);
					Assert.Equal(result, record);
				}
				finally
				{
					await context.Database.EnsureDeletedAsync();
				}
				
			}
		}

		[Fact]
		public async Task Upsert_Edit()
		{
			using (var context = new ApplicationDbContext(EntityFrameworkHelpers.SqlDb()))
			{
				await context.Database.EnsureDeletedAsync();
				await context.Database.EnsureCreatedAsync();
				await context.Database.MigrateAsync();

				var record = new SongPlaybackTracker()
				{
					Id = "foo",
					SpotifySongUri = "test-spotify-song-uri",
					State = PlaybackState.Playing,
					Duration = TimeSpan.FromSeconds(30),
					StartedUtc = DateTime.UtcNow,
					PlaybackSession = null,
					PlaybackSessionId = null
				};

				var service = new SongPlaybackRepository(new NullLogger<SongPlaybackRepository>(), context);

				await service.Upsert(record);

				record.SpotifySongUri = "new-spotify-uri";

				await service.Upsert(record);

				var response = await service.GetById("foo");

				var result = await service.GetById("foo");

				Assert.NotNull(result);
				Assert.Equal(result, record);

				await context.Database.EnsureDeletedAsync();
			}
		}
	}
}
