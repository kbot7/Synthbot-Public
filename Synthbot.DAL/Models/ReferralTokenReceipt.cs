using System;

namespace Synthbot.DAL.Models
{
	public class ReferralTokenReceipt
	{
		public string Id { get; set; }
		public string ReferralUserId { get; set; }
		public DateTime ReceivedTS { get; set; }
		public bool Claimed { get; set; }
		public DateTime ClaimedTS { get; set; }
		public string ReferrerSignalrUser { get; set; }
		public bool ReplySent { get; set; }
		public bool ReplyError { get; set; }

		// Relationships
		public string SynthbotUserId { get; set; }
		public SynthbotUser SynthbotUser { get; set; }

	}
}
