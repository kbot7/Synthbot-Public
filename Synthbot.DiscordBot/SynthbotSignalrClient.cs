using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using Synthbot.Common.Authentication;
using Synthbot.Common.SignalR;
using Synthbot.Common.SignalR.Models;
using Synthbot.DAL.Models;

namespace Synthbot.DiscordBot
{
	/// <summary>
	/// This must be a singleton
	/// </summary>
	public class SynthbotSignalrClient
	{
		private readonly ILogger<SynthbotSignalrClient> _logger;
		private readonly HubConnection _connection;
		private readonly SpotifyWebAPI _spotifyApi;
		private readonly IConfiguration _config;
		private readonly DiscordSocketClient _discordClient;
		public SynthbotSignalrClient(
			ILogger<SynthbotSignalrClient> logger,
			IHubConnectionBuilder hubConnectionBuilder,
			SpotifyWebAPI spotifyApi,
			IConfiguration config,
			DiscordSocketClient discordClient)
		{
			_config = config;
			_logger = logger;
			_connection = hubConnectionBuilder.Build();
			_spotifyApi = spotifyApi;
			_discordClient = discordClient;
			RegisterHandlers();
		}

		public async Task<bool> StartAsync()
		{
			bool started = false;
			int tryLimit = int.TryParse(_config["SynthbotSignalR:StartTryLimit"], out int parsedLimit) ? parsedLimit : 3;
			int currentTry = 1;
			while (!started && currentTry <= tryLimit)
			{
				try
				{
					await _connection.StartAsync();
					await InvokePing("SignalR Initial Start Ping");
					started = true;
					_logger.Log(LogLevel.Information, "SignalR connection successfully started");
				}
				catch (Exception)
				{
					if (currentTry == tryLimit)
					{
						_logger.Log(LogLevel.Critical, "SignalR connection failed to start on attempt {currentTry} of {tryLimit}. Retry limit reached. Abandoning connection", currentTry, tryLimit);
					}
					else
					{
						var delayMs = currentTry * 1000;
						_logger.Log(LogLevel.Critical, "SignalR connection failed to start on attempt {currentTry} of {tryLimit} Waiting {ms}ms to reconnect", currentTry, tryLimit, delayMs);
						await Task.Delay(delayMs);
					}
					currentTry++;
				}
			}
			return started;
		}

		public Task InvokePing(string pingMessage)
		{
			return _connection.InvokeAsync(SignalrMethodNames.Ping, pingMessage);
		}

		private void RegisterHandlers()
		{
			_connection.Closed += ConnectionOnClosedAsync;

			_connection.On<PingResponse>(SignalrMethodNames.Ping, PingHandler);

			_connection.On<TokenPayload>(SignalrMethodNames.InitialRegistration, HandleFirstRegistration);
			_connection.On<TokenPayload>(SignalrMethodNames.Reauthenticated, ReauthenticatedHandler);
			_connection.On<PlaybackSession, IEnumerable<FullTrack>>(SignalrMethodNames.PlaybackStarted, PlaybackStartedHandler);
		}

		private void PlaybackStartedHandler(PlaybackSession session, IEnumerable<FullTrack> tracks)
		{
			var fullTracks = tracks.Take(2).ToArray();
			_logger.Log(LogLevel.Information, "New playback started for session: {session}. New track: {trackInfo}", JsonConvert.SerializeObject(session), JsonConvert.SerializeObject(fullTracks.First()));

			// To set the channel, run "@Bot set-update-channel" in the channel you want updates posted to
			var success = ulong.TryParse(session.UpdateChannelDiscordId, out ulong id);
			if (success)
			{
				var updateChannel = _discordClient.GetChannel(id) as IMessageChannel;
				var currentPlaylist = _spotifyApi.GetPlaylist(null, session.SpotifyPlaylistId);

				updateChannel?.SendMessageAsync("", false, EmbedFactory.NowPlaying(session.DiscordVoiceChannelId, fullTracks[0], fullTracks[1], currentPlaylist)).Wait();
			}
		}

		private async Task ConnectionOnClosedAsync(Exception arg)
		{
			var delayMs = new Random().Next(0, 5) * 1000;
			_logger.Log(LogLevel.Critical, "SignalR connection lost. Waiting {ms}ms to reconnect", delayMs);
			await Task.Delay(delayMs);
			await _connection.StartAsync();
		}

		private void ReauthenticatedHandler(TokenPayload payload)
		{
			_logger.Log(LogLevel.Information, $"Discord UserName: {payload.DiscordUserId} re-authenticated from Synthbot.WebApp");

			var discordUserId = ulong.Parse(payload.DiscordUserId);
			var user = _discordClient.GetUser(discordUserId);
			user.GetOrCreateDMChannelAsync().ContinueWith(async task =>
			{
				var channel = await task;
				await channel.SendMessageAsync("You have successfully re-authenticated");
			});
		}

		private void HandleFirstRegistration(TokenPayload payload)
		{
			_logger.Log(LogLevel.Information, $"Discord UserName: {payload.DiscordUserId} initially registered from Synthbot.WebApp");

			var discordUserId = ulong.Parse(payload.DiscordUserId);
			var user = _discordClient.GetUser(discordUserId);
			user.GetOrCreateDMChannelAsync().ContinueWith(async task =>
			{
				var channel = await task;
				await channel.SendMessageAsync("You have successfully registered");
			});
		}

		private void PingHandler(PingResponse pingResponse)
		{
			_logger.Log(LogLevel.Information, "SignalR PingResponse: {pingResponse}", JsonConvert.SerializeObject(pingResponse));
		}
	}
}
