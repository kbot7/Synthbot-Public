using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;

namespace Synthbot.WebApp.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(AuthenticationSchemes = "discord-bot")]
	public class DiscordUserController : ControllerBase
	{
		private readonly DiscordUserRepository _discordUserRepo;
		private readonly PlaybackSessionRepository _playbackRepo;
		public DiscordUserController(DiscordUserRepository discordUserRepo, PlaybackSessionRepository playbackRepo)
		{
			_discordUserRepo = discordUserRepo;
			_playbackRepo = playbackRepo;
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> GetById(string discordUserId)
		{
			var discordUser = await _discordUserRepo.GetById(discordUserId);
			if (discordUser == null)
			{
				return NotFound();
			}
			return Ok(discordUser);
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> SetStatus(string discordUserId, DiscordUserStatus status)
		{
			var discordUser = await _discordUserRepo.GetById(discordUserId);
			if (discordUser == null)
			{
				await _discordUserRepo.Upsert(new DiscordUser()
				{
					DiscordUserId = discordUserId,
					InvitedTs = DateTime.UtcNow,
					UserStatus = status
				});
				return Ok();
			}
			else
			{
				discordUser.UserStatus = status;
				await _discordUserRepo.Upsert(discordUser);
				return Ok();
			}
		}

		[HttpGet]
		[Route("[action]")]
		public async Task<IActionResult> GetStatus(string discordUserId)
		{
			var discordUser = await _discordUserRepo.GetById(discordUserId);
			if (discordUser == null)
			{
				await _discordUserRepo.Upsert(new DiscordUser()
				{
					DiscordUserId = discordUserId, UserStatus = DiscordUserStatus.New
				});
				return Ok(DiscordUserStatus.New);
			}
			else
			{
				return Ok(discordUser.UserStatus);
			}
		}
	}
}
