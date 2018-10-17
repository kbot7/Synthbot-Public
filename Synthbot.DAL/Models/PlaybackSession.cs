using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Synthbot.DAL.Interfaces;

namespace Synthbot.DAL.Models
{
	public class PlaybackSession : IInsertable
	{
		[Key]
		public string Id { get; set; }
		[Required]
		public string DiscordVoiceChannelId { get; set; }
		[Required]
		public string SpotifyPlaylistId { get; set; }
		public string UpdateChannelDiscordId { get; set; }

		// Related Data
		[ForeignKey("CurrentSongPlayback")]
		public string CurrentSongPlaybackId { get; set; }
		public SongPlaybackTracker CurrentSongPlayback { get; set; }

		public IEnumerable<SongPlaybackTracker> SongPlaybacks { get; set; }

		public IEnumerable<SynthbotUser> JoinedUsers { get; set; }

		public bool IsValidForInsert()
		{
			var valid = 
				!string.IsNullOrWhiteSpace(DiscordVoiceChannelId) &&
				!string.IsNullOrWhiteSpace(SpotifyPlaylistId);

			return valid;
		}
	}
}
