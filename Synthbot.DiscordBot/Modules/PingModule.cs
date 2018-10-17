using System.Threading.Tasks;
using Discord.Commands;

namespace Synthbot.DiscordBot.Modules
{
	public class PingModule : ModuleBase<SocketCommandContext>
	{
		[Command("ping")]
		[Summary("Send a basic ping message to the bot to check if it is up.")]
		public Task Info() => ReplyAsync("Pong!");
	}
}
