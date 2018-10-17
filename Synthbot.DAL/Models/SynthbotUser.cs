using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Synthbot.DAL.Models
{
	public class SynthbotUser : IdentityUser
	{
		public string DefaultSpotifyDevice { get; set; }
		public bool AutoJoin { get; set; }

		public IEnumerable<IdentityUserLogin<string>> UserLogins { get; set; }
		public IEnumerable<IdentityUserToken<string>> UserTokens { get; set; }
		public IEnumerable<IdentityUserClaim<string>> UserClaims { get; set; }

		
		public string ActivePlaybackSessionId { get; set; }
		public PlaybackSession ActivePlaybackSession { get; set; }

		[Required]
		public string DiscordUserId { get; set; }
		public DiscordUser DiscordUser { get; set; }
	}
}
