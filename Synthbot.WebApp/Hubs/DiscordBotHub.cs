using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Synthbot.Common.SignalR;
using Synthbot.Common.SignalR.Models;

namespace Synthbot.WebApp.Hubs
{
	[Authorize(AuthenticationSchemes = "discord-bot")]
	public class DiscordBotHub : Hub
	{
		public Task Ping(string replyMessage)
		{
			var response = new PingResponse
			{
				PingMessage = replyMessage,
				UserName = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
				ReceivedAt = DateTime.UtcNow
			};

			return Clients.Caller.SendAsync(SignalrMethodNames.Ping, response);
		}
	}
}
