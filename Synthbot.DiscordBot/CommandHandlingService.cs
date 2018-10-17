using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Synthbot.DiscordBot.Services;
using Synthbot.WebApp.Client;

namespace Synthbot.DiscordBot
{
	public class CommandHandlingService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private readonly SpotifyInfoService _spotifyInfoService;
		private readonly SynthbotRestClient _synthbotWebClient;
		private IServiceProvider _provider;

		public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, SpotifyInfoService spotifyInfoService, SynthbotRestClient synthbotWebClient)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_spotifyInfoService = spotifyInfoService;
			_synthbotWebClient = synthbotWebClient;

			_discord.UserVoiceStateUpdated += UserVoiceChannelEventHandlerAsync;

			_discord.MessageReceived += MessageReceived;
		}

		private async Task UserVoiceChannelEventHandlerAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
		{
			var oldChannel = oldState.VoiceChannel;
			var newChannel = newState.VoiceChannel;

			var joinedChannel = oldChannel == null && newChannel != null;
			var changedChannel = oldChannel != null && newChannel != null;
			var exitedChannel = oldChannel != null && newChannel == null;

			// This gets fired when users mute/unmute
			var sameChannel = oldChannel == newChannel;
			if (sameChannel)
			{
				return;
			}

			using (var serviceScope = _provider.CreateScope())
			{
				var services = serviceScope.ServiceProvider;

				// Set user context for use by scoped services
				var contextAccessor = services.GetService<DiscordContextAccessor>();
				contextAccessor.User = user;

				// Get handler
				var voiceStatusChangedHandler = services.GetService<VoiceStatusChangedHandler>();

				// Handle joined
				if (joinedChannel)
				{
					await voiceStatusChangedHandler.HandleJoined(user, newChannel);
					return;
				}

				// Handle changed
				if (changedChannel)
				{
					await voiceStatusChangedHandler.HandleMoved(user, newChannel, oldChannel);
					return;
				}

				// Handle Exited channel
				if (exitedChannel)
				{
					await voiceStatusChangedHandler.HandleExit(user, oldChannel);
					return;
				}
			}
		}

		public async Task InitializeAsync(IServiceProvider provider)
		{
			_provider = provider;
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
		}

		private async Task MessageReceived(SocketMessage rawMessage)
		{
			// Ignore system messages and messages from bots
			if (!(rawMessage is SocketUserMessage message)) return;
			if (message.Source != MessageSource.User) return;

			int argPos = 0;

			var hasMention = (message.HasMentionPrefix(_discord.CurrentUser, ref argPos));
			var isDm = message.Channel is IDMChannel;

			if (!(hasMention || isDm)) return;

			var context = new SocketCommandContext(_discord, message);

			var commandContextAccessor = _provider.GetService<DiscordContextAccessor>();
			commandContextAccessor.CommandContext = context;

			var result = await _commands.ExecuteAsync(context, argPos, _provider);

			if (!result.IsSuccess && result.Error.HasValue)
			{
				switch (result.Error.Value)
				{
					case CommandError.BadArgCount:
						await context.Channel.SendMessageAsync($"Missing parameters. Use \"help 'command name'\" to see the required parameters");
						break;
					case CommandError.MultipleMatches:
						await context.Channel.SendMessageAsync($"Multiple commands match the requested message {result}");
						break;
					case CommandError.UnknownCommand:
						break;
					case CommandError.UnmetPrecondition:
						await context.Channel.SendMessageAsync(
							$"A command condition has not been met. Does the bot have enough permissions? Message: {result}");
						break;
					default:
						await context.Channel.SendMessageAsync(result.ToString());
						break;
				}
			}
		}
	}
}
