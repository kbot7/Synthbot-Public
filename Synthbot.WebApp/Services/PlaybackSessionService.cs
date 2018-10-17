using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.WebApp.Jobs;

namespace Synthbot.WebApp.Services
{
	/// <summary>
	/// This should be a singleton
	/// </summary>
	public class PlaybackSessionService
	{
		private readonly ILogger<PlaybackSessionService> _logger;
		private readonly PlaybackSessionRepository _sessionRepo;
		private readonly SongPlaybackRepository _songRepo;
		private readonly IScheduler _jobScheduler;
		private readonly SpotifyPlaybackService _playbackService;
		private readonly UserService _userService;
		public PlaybackSessionService(
			ILogger<PlaybackSessionService> logger,
			PlaybackSessionRepository sessionRepo,
			SongPlaybackRepository songRepo,
			IScheduler jobScheduler,
			SpotifyPlaybackService playbackService,
			UserService userService)
		{
			_logger = logger;
			_sessionRepo = sessionRepo;
			_songRepo = songRepo;
			_jobScheduler = jobScheduler;
			_playbackService = playbackService;
			_userService = userService;
		}

		public async Task<bool> RemoveUserAsync(string userId, string roomId)
		{
			var user = await _userService.GetUserBySynthbotUserIdAsync(userId);
			if (user == null)
			{
				_logger.LogInformation("User does not exist. UserId: {userId}", userId);
				return false;
			}

			var session = await _sessionRepo.GetByDiscordIdAsync(roomId);
			if (session == null)
			{
				_logger.LogInformation("Session does not exist for DiscordRoomId: {roomId}", roomId);
				return false;
			}

			await _userService.RemovePlaybackSession(user);

			if (!session.JoinedUsers?.Any() ?? true)
			{
				var songTacker = session.CurrentSongPlayback;
				if (songTacker != null)
				{
					songTacker.State = PlaybackState.Completed;
					await _songRepo.Upsert(songTacker);

					session.CurrentSongPlayback = null;
					session.CurrentSongPlaybackId = null;
					await _sessionRepo.UpsertSession(session);
				}
			}

			await _playbackService.PauseSpotifyUserPlaybackAsync(userId);
			return true;
		}

		public async Task<bool> JoinUserAsync(
			string userId,
			string roomId)
		{
			var user = await _userService.GetUserBySynthbotUserIdAsync(userId);
			if (user == null)
			{
				_logger.LogInformation("User does not exist. UserId: {userId}", userId);
				return false;
			}

			var session = await _sessionRepo.GetByDiscordIdAsync(roomId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true});
			if (session == null)
			{
				_logger.LogInformation("Session does not exist for DiscordRoomId: {roomId}", roomId);
				return false;
			}

			await _userService.SetPlaybackSession(user, session);

			if (session.CurrentSongPlayback == null)
			{
				await StartNewPlayback(session);
				return true;
			}

			if (session.CurrentSongPlayback.State == PlaybackState.Playing || session.CurrentSongPlayback.State == PlaybackState.Resumed)
			{
				var startMs = Convert.ToInt32((DateTime.UtcNow - session.CurrentSongPlayback.StartedUtc).TotalMilliseconds);

				await _playbackService.StartSpotifyUserPlaybackAsync(userId, session.CurrentSongPlayback.SpotifySongUri,
						startMs);
			}
			return true;
		}

		public async Task<bool> PauseAsync(PlaybackSession session)
		{
			var jobId = session.CurrentSongPlayback.JobId;
			var jobKey = new JobKey(jobId);
			var job = await _jobScheduler.GetJobDetail(jobKey);

			if (job.JobType == typeof(SongFinishedJob))
			{
				var songTracker = session.CurrentSongPlayback;

				TimeSpan pausedAtDuration = (songTracker.State == PlaybackState.Resumed && songTracker.ResumedUtc.HasValue && songTracker.PausedAtMs.HasValue)
					? DateTime.UtcNow - songTracker.ResumedUtc.Value + TimeSpan.FromMilliseconds(songTracker.PausedAtMs.Value)
					: DateTime.UtcNow - songTracker.StartedUtc;

				songTracker.PausedAtMs = Convert.ToInt32(pausedAtDuration.TotalMilliseconds);
				songTracker.PausedUtc = DateTime.UtcNow;
				songTracker.State = PlaybackState.Paused;

				await _songRepo.Upsert(songTracker);

				await _jobScheduler.DeleteJob(jobKey);

				await _playbackService.PauseSpotifyUserPlaybackAsync(session.JoinedUsers.Select(u => u.Id));

				return true;
			}
			else
			{
				throw new InvalidOperationException(
					"PlaybackSession cant be paused if its job type is not SongFinishedJob");
			}
		}

		public async Task<bool> ResumeAsync(PlaybackSession session)
		{
			if (session.CurrentSongPlayback != null && session.CurrentSongPlayback.State == PlaybackState.Paused && session.CurrentSongPlayback.PausedAtMs.HasValue)
			{
				var userIds = session.JoinedUsers.Select(u => u.Id);

				// Nothing to do if there is no more joined users, or playlist
				if (!session.JoinedUsers.Any() ||
					string.IsNullOrWhiteSpace(session.SpotifyPlaylistId))
				{
					_logger.Log(LogLevel.Information, "No more users on {sessionId}. Halting playback", session.Id);
					return false;
				}

				var songTracker = session.CurrentSongPlayback;

				songTracker.State = PlaybackState.Resumed;
				songTracker.ResumedUtc = DateTime.UtcNow;

				await _songRepo.Upsert(songTracker);

				var startPlaybackTask = _playbackService.StartSpotifyUserPlaybackAsync(userIds, session.CurrentSongPlayback.SpotifySongUri, session.CurrentSongPlayback.PausedAtMs ?? 0);

				// Spawn next job
				var jobId = $"SongFinishedJob-{Guid.NewGuid().ToString()}";
				session.CurrentSongPlayback.JobId = jobId;
				await _songRepo.Upsert(session.CurrentSongPlayback);

				var job = JobBuilder.Create<SongFinishedJob>()
					.WithIdentity(jobId)
					.WithDescription($"SongFinishedJob SessionId: {session.Id}, PlaylistId: {session.SpotifyPlaylistId}, SongUri: {session.CurrentSongPlayback.SpotifySongUri}")
					.UsingJobData("PlaybackSessionId", session.Id)
					.UsingJobData("SpotifySongUri", session.CurrentSongPlayback.SpotifySongUri)
					.Build();
				var trigger = TriggerBuilder.Create()
					.ForJob(job)
					.StartAt(session.CurrentSongPlayback.ExpectedFinishUtc)
					.Build();
				var scheduleJobTask = _jobScheduler.ScheduleJob(job, trigger);

				await Task.WhenAll(startPlaybackTask, scheduleJobTask);

				return true;
			}
			else
			{
				throw new InvalidOperationException(
					"PlaybackSession cant be resumed if it is not paused");
			}
		}

		public async Task<bool> SkipAsync(PlaybackSession session)
		{
			var jobId = session.CurrentSongPlayback.JobId;
			var jobKey = new JobKey(jobId);
			var job = await _jobScheduler.GetJobDetail(jobKey);

			if (job != null)
			{
				session.CurrentSongPlayback.State = PlaybackState.Skipped;
				await _songRepo.Upsert(session.CurrentSongPlayback);

				var triggers = await _jobScheduler.GetTriggersOfJob(jobKey);

				await _jobScheduler.TriggerJob(jobKey, job.JobDataMap);
				await _jobScheduler.UnscheduleJobs(triggers.Select(t => t.Key).ToImmutableList());
				
				return true;
			}
			else
			{
				// Start new playback if there is no scheduled job
				await StartNewPlayback(session);
				return true;
			}
		}

		public async Task<bool> ChangePlaylist(PlaybackSession session, string newSpotifyPlaylistId)
		{
			session.SpotifyPlaylistId = newSpotifyPlaylistId;
			await _sessionRepo.UpsertSession(session);

			// If there is current playback, remove it's current job, and create a new one
			if (session.CurrentSongPlayback != null)
			{
				var songTracker = session.CurrentSongPlayback;
				songTracker.State = PlaybackState.Skipped;
				await _songRepo.Upsert(songTracker);

				await _jobScheduler.DeleteJob(new JobKey(session.CurrentSongPlayback.JobId));
			}
			await StartNewPlayback(session);

			return true;
		}

		public async Task Reset()
		{
			await _sessionRepo.Reset();
			var jobKeys = await _jobScheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
			await _jobScheduler.DeleteJobs(jobKeys);
		}

		public Task SetTextUpdateChannel(PlaybackSession session, string updateChannelDiscordId)
		{
			session.UpdateChannelDiscordId = updateChannelDiscordId;
			return _sessionRepo.UpsertSession(session);
		}

		private async Task StartNewPlayback(PlaybackSession session)
		{
			var jobId = $"SongFinishedJob-{Guid.NewGuid().ToString()}";
			var job = JobBuilder.Create<SongFinishedJob>()
				.WithIdentity(jobId)
				.WithDescription($"SongFinishedJob SessionId: {session.Id}")
				.UsingJobData("PlaybackSessionId", session.Id.ToString())
				.Build();
			var trigger = TriggerBuilder.Create()
				.ForJob(job)
				.StartNow()
				.Build();
			await _jobScheduler.ScheduleJob(job, trigger);
		}
	}
}
