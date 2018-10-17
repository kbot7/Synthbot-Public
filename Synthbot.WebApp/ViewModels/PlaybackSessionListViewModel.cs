using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Synthbot.DAL.Models;

namespace Synthbot.WebApp.ViewModels
{
	public class PlaybackSessionListViewModel
	{
		public IEnumerable<PlaybackSession> PlaybackSessions { get; set; }
	}
}
