using System;
using System.Collections.Generic;
using System.Text;

namespace Synthbot.DAL.Models
{
	public class PlaybackSessionInfo
	{
		public string SpotifyPlaylistId { get; set; }
		public string DiscordVoiceChannelId { get; set; }
	}
}
