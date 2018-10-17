using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synthbot.Common;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.WebApp.Services;

namespace Synthbot.WebApp.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(AuthenticationSchemes = "discord-bot")]
	public class SynthChannelController : ControllerBase
	{
		private readonly ILogger<SynthChannelController> _logger;
		private readonly UserManager<SynthbotUser> _userManager;
		private readonly UserService _userService;
		private readonly PlaybackSessionService _playbackSessionService;
		private readonly PlaybackSessionRepository _playbackRepo;
		private readonly UserTokenService _tokenService;
		public SynthChannelController(
			ILogger<SynthChannelController> logger,
			UserManager<SynthbotUser> userMgr,
			UserService userService,
			PlaybackSessionService playbackSessionService,
			UserTokenService tokenService,
			PlaybackSessionService sessionService,
			PlaybackSessionRepository playbackRepo)
		{
			_logger = logger;
			_userManager = userMgr;
			_userService = userService;
			_playbackSessionService = playbackSessionService;
			_tokenService = tokenService;
			_playbackRepo = playbackRepo;
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> GetCurrentPlayback(string voiceChannelId)
		{
			var session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });
			if (session == null)
			{
				return Ok(null);
			}
			var sessionInfo = new PlaybackSessionInfo()
			{
				SpotifyPlaylistId = session.SpotifyPlaylistId,
				DiscordVoiceChannelId = session.DiscordVoiceChannelId
			};

			return Ok(sessionInfo);
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> AddUser(string voiceChannelId)
		{
			var userId = User.GetClaimValueFromType(ClaimTypes.NameIdentifier);

			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(voiceChannelId))
			{
				return new NotFoundResult();
			}

			var result = await _playbackSessionService
				.JoinUserAsync(userId, voiceChannelId);

			if (result)
			{
				return Ok();
			}
			else
			{
				return StatusCode(500);
			}
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> RemoveUser(string voiceChannelId)
		{
			var userId = User.GetClaimValueFromType(ClaimTypes.NameIdentifier);

			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(voiceChannelId))
			{
				return new NotFoundResult();
			}

			var result = await _playbackSessionService
				.RemoveUserAsync(userId, voiceChannelId);

			if (result)
			{
				return Ok();
			}
			else
			{
				return StatusCode(500);
			}
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> PauseChannel(string voiceChannelId)
		{
			var session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions(){ IncludeCurrentPlayback = true});
			var pauseinfo = new PlayPauseInfo
			{
				TrackId = session.CurrentSongPlayback.SpotifySongUri.Remove(0, 14)
			};
			if (session == null)
			{
				return new NotFoundResult();
			}

			var result = await _playbackSessionService.PauseAsync(session);
			session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });
			pauseinfo.PausedMs = session.CurrentSongPlayback.PausedAtMs;

			if (result)
			{
				return Ok(pauseinfo);
			}
			else
			{
				return StatusCode(500);
			}
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> ResumeChannel(string voiceChannelId)
		{
			var session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });

			var resumeinfo = new PlayPauseInfo
			{
				PausedMs = session.CurrentSongPlayback.PausedAtMs,
				TrackId = session.CurrentSongPlayback.SpotifySongUri.Remove(0, 14)
			};

			if (session == null)
			{
				return new NotFoundResult();
			}

			var result = await _playbackSessionService.ResumeAsync(session);

			if (result)
			{
				return Ok(resumeinfo);
			}
			else
			{
				return StatusCode(500);
			}
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> Skip(string voiceChannelId)
		{
			var session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });
			var skipinfo = new SkipInfo
			{
				CurrentSongPlaybackId = session.CurrentSongPlayback.SpotifySongUri.Remove(0, 14),
				StartedUtc = session.CurrentSongPlayback.StartedUtc
			};
			if (session == null)
			{
				return new NotFoundResult();
			}

			var result = await _playbackSessionService.SkipAsync(session);

			if (result)
			{
				return Ok(skipinfo);
			}
			else
			{
				return StatusCode(500);
			}
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> ChangePlaylist(string voiceChannelId, string newSpotifyPlaylistId)
		{
			var session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });
			var payload = new ChangePlaylistInfo();
			if (session == null)
			{
				var playbackSession = new PlaybackSession()
				{
					DiscordVoiceChannelId = voiceChannelId,
					SpotifyPlaylistId = newSpotifyPlaylistId
				};
				await _playbackRepo.UpsertSession(playbackSession);
				return Ok(playbackSession);
			}
			else
			{
				payload.PreviousPlaylist = session.SpotifyPlaylistId;
			}

			var result = await _playbackSessionService.ChangePlaylist(session, newSpotifyPlaylistId);
			session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });
			payload.NewPlaylist = session.SpotifyPlaylistId;
			if (result)
			{
				return Ok(payload);
			}
			else
			{
				return StatusCode(500);
			}
		}

		// TODO This is just a sample of obtaining access tokens. Move this elsewhere
		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> SpotifyAccessToken()
		{
			string discordUserId = User.GetDiscordUserId();

			var user = await _userService.GetUserByDiscordIdAsync(discordUserId);
			if (user == null)
			{
				return BadRequest();
			}

			var token = await _tokenService.GetTokenAsync(user.Id);
			if (token == null)
			{
				return StatusCode(404);
			}

			return Ok(token.SpotifyAccessToken);
		}
		
		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> IsUserRegistered(string discordUserId)
		{
			string userId = await _userService.GetUserIdByDiscordIdAsync(discordUserId);
			var exists = !string.IsNullOrWhiteSpace(userId);
			if (exists)
			{
				return Ok();
			}

			return NotFound();
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> CanAutoJoin(string discordUserId)
		{
			var user = await _userService.GetUserByDiscordIdAsync(discordUserId);

			if (user != null && user.AutoJoin)
			{
				return Ok();
			}

			return NotFound();
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> SetAutoJoin(string discordUserId, bool autoJoin)
		{
			var user = await _userService.GetUserByDiscordIdAsync(discordUserId);
			
			if (user != null)
			{
				await _userService.SetAutoJoin(user, autoJoin);

				return Ok();
			}

			return NotFound();
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> SetDevice(string discordUserId, string spotifyDeviceId)
		{
			var user = await _userService.GetUserByDiscordIdAsync(discordUserId);

			if (user != null)
			{
				await _userService.SetDefaultDevice(user, spotifyDeviceId);

				return Ok();
			}

			return NotFound();
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> SetTextUpdateChannel(string voiceChannelId, string textChannelDiscordId)
		{
			var session = await _playbackRepo.GetByDiscordIdAsync(voiceChannelId, new PlaybackSessionQueryOptions() { IncludeCurrentPlayback = true });
			if (session == null)
			{
				return new NotFoundResult();
			}

			await _playbackSessionService.SetTextUpdateChannel(session, textChannelDiscordId);

			return Ok();
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> Reset()
		{
			try
			{
				await _playbackSessionService.Reset();
			}
			catch (Exception)
			{
				return StatusCode(500);
			}
			return Ok();
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> GetSynthbotUserFromDiscordId(string discordId)
		{
			try
			{
				var user = await _userService.GetUserByDiscordIdAsync(discordId);

				if (user == null)
				{
					return NotFound();
				}

				// Remove references so they aren't serialized
				// TODO handle this in global JSON settings instead (and lazy-load them from EF in the first place)
				user.ActivePlaybackSession = null;
				user.UserClaims = null;
				user.UserLogins = null;
				user.UserLogins = null;
				user.UserTokens = null;
				return Ok(user);
			}
			catch (Exception)
			{
				return StatusCode(500);
			}
		}
	}
}
