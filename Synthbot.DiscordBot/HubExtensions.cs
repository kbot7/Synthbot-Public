using System;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synthbot.Common.Authentication;

namespace Synthbot.DiscordBot
{
	public static class HubExtensions
	{
		public static void RegisterDiscordbotHandlers(this HubConnection connection, IServiceProvider services)
		{
			// Get Services
			var logger = services.GetService<ILoggerFactory>().CreateLogger("DiscordBotSignalRHandlers");

			connection.On<string>("ping", (string msg) =>
			{
				logger.Log(LogLevel.Information, msg);
			});

			connection.On<TokenPayload>("AuthReply", payload =>
			{
				logger.Log(LogLevel.Information, $"AuthReply Received for SpotifyUserId: {payload.SpotifyUserId}");
				// TODO: message the user in discord to confirm they've added spotify
			});
		}
	}
}
