using System;
using System.Collections.Generic;
using System.Text;

namespace Synthbot.DAL.Models
{
	public class PlayPauseInfo
	{
		public int? PausedMs { get; set; }
		public string TrackId { get; set; }
	}
}
