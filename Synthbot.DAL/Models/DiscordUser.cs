using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Synthbot.DAL.Models
{
	public enum DiscordUserStatus
	{
		[Description("No Response")]
		NoResponse = 0,
		[Description("Registered with notifications on")]
		RegisteredWithNotify,
		[Description("Registered with notifications off")]
		RegisteredWithoutNotify,
		[Description("New")]
		New
	}

	public class DiscordUser
	{
		[Key]
		public string DiscordUserId { get; set; }
		public string DiscordUsername { get; set; }
		public string DiscordEmailAddress { get; set; }

		public DateTime InvitedTs { get; set; }
		public DiscordUserStatus UserStatus { get; set; }

		// Relationships
		public SynthbotUser SynthbotUser { get; set; }

	}
}
