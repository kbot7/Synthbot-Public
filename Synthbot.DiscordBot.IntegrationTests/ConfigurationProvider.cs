using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Synthbot.DiscordBot.IntegrationTests
{
	public class ConfigurationProvider
	{
		private readonly IConfiguration _config;
		public ConfigurationProvider()
		{
			_config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddUserSecrets("7d8c1353-93b2-49a8-82ad-113527cb55cc")
				.AddEnvironmentVariables()
				.Build();
		}

		// Singleton pattern code
		private static ConfigurationProvider _instance;
		public static ConfigurationProvider GetInstance()
		{
			if (_instance == null)
			{
				_instance = new ConfigurationProvider();
			}
			return _instance;
		}
		public static IConfiguration Config
		{
			get
			{
				return GetInstance()._config;
			}
		}
	}
}
