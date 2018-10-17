using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Synthbot.Common;
using Synthbot.Common.SignalR;
using Synthbot.DiscordBot.Services;
using Synthbot.WebApp.Client;

namespace Synthbot.DiscordBot
{
	static class Program
	{
		public static async Task Main()
		{
			Console.Title = "Synthbot.DiscordBot";

			// Add configuration sources
			var config = AddConfiguration();

			// Configure logging
			var loggerConfig = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.Enrich.WithThreadId()
				.WriteTo.Console()
				.WriteTo.ApplicationInsightsEvents(config["APPINSIGHTS_INSTRUMENTATIONKEY"]);
			if (config["synthbot.storage.connectionstring"] != null &&
			    config["synthbot.storage.log.bot.tablename"] != null)
			{
				loggerConfig.WriteTo.AzureTableStorage(config["synthbot.storage.connectionstring"],
					storageTableName: config["synthbot.storage.log.bot.tablename"]);
			}
			Log.Logger = loggerConfig.CreateLogger();

			// Configure Dependency Injection Services
			var services = await ConfigureServicesAsync(config);

			// Start SignalR Client
			var signalrClient = services.GetRequiredService<SynthbotSignalrClient>();
			await signalrClient.StartAsync();

			// Start DiscordSocketClient
			var discordClient = services.GetRequiredService<DiscordSocketClient>();
			await discordClient.LoginAsync(TokenType.Bot, config["discord.bot.token"]);
			await discordClient.StartAsync();

			// Keep app alive
			await Task.Delay(-1);
		}

		private static IConfiguration AddConfiguration()
		{
			return new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddUserSecrets("7d8c1353-93b2-49a8-82ad-113527cb55cc")
				.AddEnvironmentVariables()
				.Build();
		}

		private static async Task<IServiceProvider> ConfigureServicesAsync(IConfiguration config)
		{
			var serviceCollection = new ServiceCollection()
				.AddLogging(conf => conf.AddSerilog())
				.AddSingleton(config)
				.AddSingleton<DiscordSocketClient>()
				.AddSingleton<CommandService>()
				.AddSingleton<CommandHandlingService>()
				.AddSingleton<InteractiveService>()
				.AddSingleton<SpotifyInfoService>()
				.AddTransient<DiscordContextAccessor>()
				.AddLogging()
				.AddSingleton<DiscordLogService>()
				.AddScoped<VoiceStatusChangedHandler>()
				.AddSynthbotClients(config)
				.AddSpotifyClient(config);

			var services = serviceCollection.BuildServiceProvider();

			// This looks stupid, but it's required in order to instantiate the DiscordLogService into the Discord services
			// It was from the discord.net sample. Let's fix it up later
			services.GetRequiredService<DiscordLogService>();
			await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);
			services.GetService<ILoggerFactory>();

			return services;
		}

		public static IServiceCollection AddSpotifyClient(this IServiceCollection services, IConfiguration config)
		{
			return services.AddTransient<SpotifyWebAPI>(serviceProvider =>
			{
				var spotifyClientCredentialsAuth = new CredentialsAuth(
					config["spotify.api.clientid"],
					config["spotify.api.clientsecret"]);

				var spotifyClientCredsToken = spotifyClientCredentialsAuth.GetToken().Result;

				var spotifyApiInstance = new SpotifyWebAPI()
				{
					AccessToken = spotifyClientCredsToken.AccessToken,
					TokenType = spotifyClientCredsToken.TokenType,
					UseAuth = true
				};

				return spotifyApiInstance;
			});
		}

		public static IServiceCollection AddSynthbotClients(this IServiceCollection services, IConfiguration config)
		{
			return services
				.AddTransient<ISynthbotAuthenticator>(provider =>
					new SynthbotDiscordUserAuthenticator(
						provider.GetService<DiscordContextAccessor>()?.User?.Id.ToString() ?? "",
						config["synthbot.token.sharedsecret"],
						provider.GetService<DiscordContextAccessor>()?.User?.Username ?? ""))
				.AddTransient<SynthbotRestClient>(provider =>
					new SynthbotRestClient(
						new Uri($"{config["synthbot.webapp.protocol"]}://{config["synthbot.webapp.host"]}"),
						provider.GetRequiredService<ISynthbotAuthenticator>()))
				.AddSingleton<SynthbotSignalrClient>(provider =>
				{
					var builder = new HubConnectionBuilder()
						.WithUrl($"{config["synthbot.webapp.protocol"]}://{config["synthbot.webapp.host"]}/bot-hub",
							options =>
							{
								options.Transports = HttpTransportType.WebSockets;
								options.AccessTokenProvider = () =>
								{
									return Task.FromResult(JwtBuilder.BuildDiscordJwtForSignalR(
										config["synthbot.token.sharedsecret"], SignalrUsernames.BotUsername));
								};
							})
						.ConfigureLogging(logging => { logging.AddSerilog(); });
						//.AddMessagePackProtocol();
					return new SynthbotSignalrClient(
						provider.GetService<ILogger<SynthbotSignalrClient>>(),
						builder,
						provider.GetService<SpotifyWebAPI>(),
						config,
						provider.GetService<DiscordSocketClient>());
				});
		}
	}
}
