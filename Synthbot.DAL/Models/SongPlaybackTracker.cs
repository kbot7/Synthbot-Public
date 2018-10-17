using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Synthbot.DAL.Interfaces;

namespace Synthbot.DAL.Models
{
	public class SongPlaybackTracker : IInsertable
	{
		[Key]
		public string Id { get; set; }
		[Required]
		public string SpotifySongUri { get; set; }
		public string JobId { get; set; }
		public DateTime StartedUtc { get; set; }
		public TimeSpan Duration { get; set; }
		public PlaybackState State { get; set; }
		public int? PausedAtMs { get; set; }
		public DateTime? PausedUtc { get; set; }
		public DateTime? ResumedUtc { get; set; }

		[NotMapped]
		public DateTime ExpectedFinishUtc
		{
			get
			{
				if (StartedUtc != default(DateTime) && Duration != default(TimeSpan))
				{
					if (PausedAtMs.HasValue)
					{
						var remainingDuration = Duration - TimeSpan.FromMilliseconds(PausedAtMs.Value);
						return ResumedUtc?.Add(remainingDuration) ?? DateTime.UtcNow;
					}

					TimeSpan duration = Duration;
					if (PausedAtMs.HasValue)
					{
						duration = duration - TimeSpan.FromMilliseconds(PausedAtMs.Value);
					}
					return StartedUtc.Add(duration);
				}
				return default(DateTime);
			}
		}

		[ForeignKey("ActivePlaybackSession")]
		public string PlaybackSessionId { get; set; }
		public PlaybackSession PlaybackSession { get; set; }

		public bool IsValidForInsert()
		{
			var valid =
				!string.IsNullOrWhiteSpace(SpotifySongUri);

			return valid;
		}
	}
}
