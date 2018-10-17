using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Quartz;
using SpotifyAPI.Web.Models;
using Synthbot.Common.SignalR;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.WebApp.Hubs;
using Synthbot.WebApp.Services;

namespace Synthbot.WebApp.Jobs
{
	public class SongFinishedJob : IJob
	{
		private readonly ILogger<SongFinishedJob> _logger;
		private readonly PlaybackSessionRepository _sessionRepo;
		private readonly SongPlaybackRepository _songRepo;
		private readonly SpotifyHttpClientFactory _spotifyApiFactory;
		private readonly SpotifyPlaybackService _playbackService;
		private readonly IScheduler _jobScheduler;
		private readonly IHubContext<DiscordBotHub> _botHub;
		public SongFinishedJob(
			ILogger<SongFinishedJob> logger,
			PlaybackSessionRepository sessionRepo,
			SongPlaybackRepository songRepo,
			SpotifyHttpClientFactory spotifyApiFactory,
			UserTokenService tokenService,
			SpotifyPlaybackService playbackService,
			IScheduler jobScheduler,
			IHubContext<DiscordBotHub> botHub)
		{
			_logger = logger;
			_sessionRepo = sessionRepo;
			_songRepo = songRepo;
			_spotifyApiFactory = spotifyApiFactory;
			_playbackService = playbackService;
			_jobScheduler = jobScheduler;
			_botHub = botHub;
		}

		// TODO lots of room for parallelism in this method. StartSpotifyUserPlaybackAsync could be run in parallel early in the method
		public async Task Execute(IJobExecutionContext context)
		{
			_logger.LogInformation("Starting SongFinishedJob for {id}", context.JobDetail.Key.Name);

			// Get job data
			var jobData = context.JobDetail.JobDataMap;
			var playbackSessionId = jobData.GetString("PlaybackSessionId");
			var finishedSpotifySongUri = jobData.GetString("SpotifySongUri");

			// Get PlaybackSession from Id
			var session = await _sessionRepo.GetById(playbackSessionId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true});

			if (session == null)
			{
				_logger.Log(LogLevel.Information, "Session ({sessionId}) no longer exists. Halting playback", playbackSessionId);
				return;
			}

			if (!string.IsNullOrWhiteSpace(finishedSpotifySongUri) && session.CurrentSongPlayback != null)
			{
				// Mark the last song as completed or skipped
				var lastSongPlayback = session.CurrentSongPlayback;
				lastSongPlayback.State = lastSongPlayback.State == PlaybackState.Skipped ? lastSongPlayback.State : PlaybackState.Completed;
				await _songRepo.Upsert(lastSongPlayback);
				session.CurrentSongPlayback = null;
				session.CurrentSongPlaybackId = null;
			}

			// Nothing to do if there is no more joined users, or playlist
			if (!(session.JoinedUsers?.Any() ?? false) ||
				string.IsNullOrWhiteSpace(session.SpotifyPlaylistId))
			{
				_logger.Log(LogLevel.Information, "No more users on {sessionId}. Clearing current playback", session.Id);

				// Commit the empty CurrentSongPlayback to the db
				session.CurrentSongPlayback = null;
				session.CurrentSongPlaybackId = null;
				await _sessionRepo.UpsertSession(session);

				return;
			}

			var nextTracks = await GetNextTracksAsync(session, finishedSpotifySongUri);
			var nextTrack = nextTracks.First();

			// Send signalr message containing next track details
			await _botHub.Clients.User(SignalrUsernames.BotUsername)
				.SendAsync(SignalrMethodNames.PlaybackStarted, session, nextTracks.Select(t => t.Track));

			// Set session's CurrentSongPlayback to the next track
			session.CurrentSongPlayback = new SongPlaybackTracker()
			{
				SpotifySongUri = nextTracks.First().Track.Uri,
				Duration = TimeSpan.FromMilliseconds(nextTrack.Track.DurationMs),
				PlaybackSessionId = session.Id,
				StartedUtc = DateTime.UtcNow
			};

			// Play next track for all connected users
			var userIds = session.JoinedUsers.Select(u => u.Id);
			await _playbackService.StartSpotifyUserPlaybackAsync(userIds, nextTrack.Track.Uri);

			// Spawn next job
			var jobId = $"SongFinishedJob-{Guid.NewGuid().ToString()}";
			var job = JobBuilder.Create<SongFinishedJob>()
				.WithIdentity(jobId)
				.WithDescription($"SongFinishedJob SessionId: {session.Id}, PlaylistId: {session.SpotifyPlaylistId}, SongUri: {nextTrack.Track.Uri}")
				.UsingJobData("PlaybackSessionId", session.Id)
				.UsingJobData("SpotifySongUri", nextTrack.Track.Uri)
				.Build();
			var trigger = TriggerBuilder.Create()
				.ForJob(job)
				.StartAt(session.CurrentSongPlayback.ExpectedFinishUtc)
				.Build();
			await _jobScheduler.ScheduleJob(job, trigger);

			session.CurrentSongPlayback.JobId = jobId;
			session.CurrentSongPlayback.State = PlaybackState.Playing;
			await _sessionRepo.UpsertSession(session);
		}

		private async Task<IEnumerable<PlaylistTrack>> GetNextTracksAsync(PlaybackSession session, string finishedSongUri = null, int previewCount = 2)
		{
			// Call spotifyApi to get playlist from playlistId
			// TODO this will have performance issues for larger playlists. Instead use the limit: and offset: params

			List<PlaylistTrack> playlistTracks;
			using (var client = await _spotifyApiFactory.CreateAppClient())
			{
				playlistTracks = (await client.GetPlaylistTracksAsync(null, session.SpotifyPlaylistId))
					.Items;
			}

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

			return nextTracks;
		}
	}
}
