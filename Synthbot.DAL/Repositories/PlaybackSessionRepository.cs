using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synthbot.DAL.Models;

namespace Synthbot.DAL.Repositories
{
	public class PlaybackSessionQueryOptions
	{
		public bool IncludeCurrentPlayback { get; set; }
		public bool IncludeJoinedUsers { get; set; }
		public bool IncludeDiscordUsers { get; set; }
	}

	public class PlaybackSessionRepository
	{
		private readonly ILogger<PlaybackSessionRepository> _logger;
		private readonly ApplicationDbContext _db;
		private readonly SongPlaybackRepository _playbackRepo;
		private readonly SemaphoreSlim _dbSignal;
		public PlaybackSessionRepository(
			ILogger<PlaybackSessionRepository> logger,
			ApplicationDbContext db,
			SongPlaybackRepository playbackRepo)
		{
			_logger = logger;
			_db = db;
			_playbackRepo = playbackRepo;
			_dbSignal = new SemaphoreSlim(1, 1);
		}

		public async Task<PlaybackSession> GetById(string id, PlaybackSessionQueryOptions queryOptions = null)
		{
			var session = await _db.PlaybackSessions.Include(e => e.JoinedUsers).FirstOrDefaultAsync(e => e.Id == id);

			await PopulatePropertiesAsync(session, queryOptions);
			return session;
		}

		public async Task<PlaybackSession> GetByDiscordIdAsync(string discordId, PlaybackSessionQueryOptions queryOptions = null)
		{
			var session = await _db.PlaybackSessions.Include(e => e.JoinedUsers)
				.FirstOrDefaultAsync(m => m.DiscordVoiceChannelId == discordId);
			await PopulatePropertiesAsync(session, queryOptions);
			return session;
		}

		public async Task UpsertSession(PlaybackSession session)
		{
			if (!session.IsValidForInsert())
			{
				throw new Exception("PlaybackSession is not valid for Upsert");
			}

			await _dbSignal.WaitAsync();

			try
			{
				var entity = await _db.PlaybackSessions
					.FirstOrDefaultAsync(e => e.DiscordVoiceChannelId == session.DiscordVoiceChannelId);

				if (entity == null)
				{
					await _db.PlaybackSessions.AddAsync(session);
					await _db.SaveChangesAsync();
				}
				else
				{
					_db.PlaybackSessions.Attach(entity);
					entity = session;
					await _db.SaveChangesAsync();
				}

				if (session.CurrentSongPlayback != null)
				{
					// Upsert the session's Id to the CurrentSongPlayback. The relationship is not created automatically
					var retrievedSession = await GetByDiscordIdAsync(session.DiscordVoiceChannelId);
					retrievedSession.CurrentSongPlayback.PlaybackSession = retrievedSession;
					await _playbackRepo.Upsert(retrievedSession.CurrentSongPlayback);
				}

				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();

			}
		}

		/// <summary>
		/// FOR TESTING ONLY
		/// </summary>
		/// <returns></returns>
		public async Task Reset()
		{
			var sessions = await _db.PlaybackSessions.ToListAsync();

			foreach (var session in sessions)
			{
				session.CurrentSongPlayback = null;
				session.CurrentSongPlaybackId = null;
				_db.PlaybackSessions.Update(session);
			}

			var users = await _db.Users.ToListAsync();

			foreach (var user in users)
			{
				user.ActivePlaybackSession = null;
				user.ActivePlaybackSessionId = null;
				_db.Users.Update(user);
			}

			await _db.SongPlaybackTrackers.ForEachAsync(t => t.State = PlaybackState.Completed);

			await _db.SaveChangesAsync();
		}

		public async Task<IList<PlaybackSession>> Get(int offset, int take, PlaybackSessionQueryOptions queryOptions = null)
		{
			IQueryable<PlaybackSession> query = _db.PlaybackSessions.AsQueryable();

			if (queryOptions != null)
			{
				if (queryOptions.IncludeJoinedUsers)
				{
					query = query.Include(e => e.JoinedUsers);
				}
				if (queryOptions.IncludeCurrentPlayback)
				{
					query = query.Include(e => e.CurrentSongPlayback);
				}
				if (queryOptions.IncludeCurrentPlayback && queryOptions.IncludeDiscordUsers)
				{
					query = query.Include("JoinedUsers.DiscordUser");
				}
			}
			
	
			return await query.Skip(offset).Take(take).ToListAsync();
		}

		private async Task PopulatePropertiesAsync(PlaybackSession session, PlaybackSessionQueryOptions queryOptions = null)
		{
			// Looks like this related property is being eager loaded. The inner if was never hit in testing
			if (session != null &&
				!string.IsNullOrWhiteSpace(session.CurrentSongPlaybackId) &&
				queryOptions != null)
			{
				var currentPlaybackId = session.CurrentSongPlaybackId;
				var playback = await _db.SongPlaybackTrackers.FirstOrDefaultAsync(m => m.Id == currentPlaybackId);
				session.CurrentSongPlayback = playback ?? throw new Exception("Current playback cannot be null");
			}
		}
	}
}
