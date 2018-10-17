using System.Threading.Tasks;
using Discord.Commands;
using Synthbot.WebApp.Client;

namespace Synthbot.DiscordBot.Modules
{
	public class InfoModule : ModuleBase<SocketCommandContext>
	{
		private readonly SynthbotRestClient _client;
		public InfoModule(SynthbotRestClient client)
		{
			_client = client;
		}

		[Command("info")]
		[Summary("Get general info about the bot")]
		public Task Pong() => ReplyAsync(
				$"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net 1.0\n");		
	}
}
