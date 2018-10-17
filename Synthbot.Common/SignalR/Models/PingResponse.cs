using System;

namespace Synthbot.Common.SignalR.Models
{
	public class PingResponse
	{
		public string UserName { get; set; }
		public string PingMessage { get; set; }
		public DateTime ReceivedAt { get; set; }
	}
}
