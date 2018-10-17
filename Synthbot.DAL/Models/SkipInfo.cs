using System;
using System.Collections.Generic;
using System.Text;

namespace Synthbot.DAL.Models
{
	public class SkipInfo
	{
		public DateTime StartedUtc { get; set; }
		public string CurrentSongPlaybackId { get; set; }
	}
}
