using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Synthbot.DAL.Models;
using Synthbot.WebApp.Client;

namespace Synthbot.DiscordBot.Modules
{
	public class SpotifyInteractiveModule : InteractiveBase<SocketCommandContext>
	{
		private readonly IConfiguration _config;
		private readonly SpotifyWebAPI _spotifyApi;
		private readonly SynthbotRestClient _synthbotWebClient;
		private readonly ILogger<SpotifyModule> _logger;
		public SpotifyInteractiveModule(
			IConfiguration config,
			SynthbotRestClient synthbotWebClient,
			ILogger<SpotifyModule> logger,
			SpotifyWebAPI spotifyApi)
		{
			_config = config;
			_synthbotWebClient = synthbotWebClient;
			_logger = logger;
			_spotifyApi = spotifyApi;
		}

		[Command("set-device", RunMode = RunMode.Async)]
		[Summary("Sends you a list of your devices and lets you pick which device to stream to.")]
		public async Task GetAndSetDevice()
		{
			var token = await _synthbotWebClient.GetSpotifyToken();
			_spotifyApi.AccessToken = token;
			var response = await _spotifyApi.GetDevicesAsync();
			if (response.HasError())
			{
				await ReplyAsync($"Error | Status: {response.Error.Status} Message: {response.Error.Message}");
				return;
			}

			var devices = response?.Devices;

			var tableBuilder = new AsciiTableBuilder<Tuple<string, string, string>>(
				new Tuple<string, int>("Name", 15),
				new Tuple<string, int>("Type", 13),
				new Tuple<string, int>("ID", 40));

			var deviceInfo = devices
				.Select(d => new Tuple<string, string, string>(d.Name, d.Type, d.Id))
				.ToList();

			var tableString = tableBuilder.GetTableString(deviceInfo);

			var devicesString = $"Reply with just the name of the device you want to use:```{tableString}```";
			await ReplyAsync(devicesString);

			var userResponse = await NextMessageAsync();
			if (userResponse == null)
			{
				await ReplyAsync("You did not reply before the timeout");
				return;
			}
			else
			{
				var deviceCheck = devices.Find(d => d.Name == userResponse.Content);
				if (deviceCheck == null)
				{
					await ReplyAsync($"{Context.User.Mention} device not found. Make sure you're entering the device name correctly.");
					return;
				}
				var result = await _synthbotWebClient.SetDeviceId(Context.User.Id.ToString(), deviceCheck.Id);
				await ReplyAsync($"Success: {result}");
			}
		}

		[Command("notify", RunMode = RunMode.Async)]
		[Summary("Turn on notifications to automatically be alerted when you join a voice channel with playback")]
		public async Task Notify()
		{
			var status = await _synthbotWebClient.GetDiscordUserStatus(Context.User.Id.ToString());
			await ReplyAsync("", false, EmbedFactory.Notifications.NotifyState(Context, status));

			var userResponse = await NextMessageAsync();
			if (userResponse != null)
			{
				DiscordUserStatus newStatus = default;
				if (userResponse.Content.ToLower() == "yes")
				{
					newStatus = DiscordUserStatus.RegisteredWithNotify;
				} else if (userResponse.Content.ToLower() == "no")
				{
					newStatus = DiscordUserStatus.RegisteredWithoutNotify;
				}

				if (newStatus != default)
				{
					await _synthbotWebClient.SetDiscordUserStatus(Context.User.Id.ToString(), newStatus);
				}
				
				await ReplyAsync("", false, EmbedFactory.Notifications.NotifyUpdate(Context, newStatus, userResponse.Content));
			}
		}
	}
}
