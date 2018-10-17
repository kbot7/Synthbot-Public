using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synthbot.DAL.Repositories;
using Synthbot.WebApp.ViewModels;

namespace Synthbot.WebApp.Controllers
{
	public class PlaybackSessionController : Controller
	{

		private readonly PlaybackSessionRepository _playbackSessionRepo;
		public PlaybackSessionController(PlaybackSessionRepository playbackSessionRepository)
		{
			_playbackSessionRepo = playbackSessionRepository;
		}

		[Authorize]
		public async Task<IActionResult> Index()
		{
			ViewData["Message"] = "Current playback sessions";

			var vm = new PlaybackSessionListViewModel()
			{
				PlaybackSessions = await _playbackSessionRepo.Get(0, 50, new PlaybackSessionQueryOptions()
				{
					IncludeJoinedUsers = true,
					IncludeCurrentPlayback = true,
					IncludeDiscordUsers = true
				})
			};

			return View(vm);
		}
	}
}
