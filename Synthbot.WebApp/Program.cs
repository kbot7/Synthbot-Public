using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Synthbot.WebApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var loggerConfig = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.WriteTo.ApplicationInsightsEvents(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
			if (Configuration["synthbot.storage.connectionstring"] != null &&
				Configuration["synthbot.storage.log.web.tablename"] != null)
			{
				loggerConfig.WriteTo.AzureTableStorage(Configuration["synthbot.storage.connectionstring"],
					storageTableName: Configuration["synthbot.storage.log.web.tablename"]);
			}
			Log.Logger = loggerConfig.CreateLogger();

			try
			{
				BuildWebHost(args).Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Host terminated unexpectedly");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
			.AddJsonFile("hosting.json", optional: false)
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.AddUserSecrets("7d8c1353-93b2-49a8-82ad-113527cb55cc")
			.AddEnvironmentVariables()
			.Build();

		public static IWebHost BuildWebHost(string[] args)
		{
			var host = WebHost.CreateDefaultBuilder(args)
				.UseConfiguration(Configuration)
				//.UseSerilog()
				.UseKestrel()
				.UseStartup<Startup>()
				.ConfigureLogging((context, logging) =>
				{
					logging.ClearProviders();
					logging.AddSerilog();
				})
				.Build();

			return host;
		}
	}
}
