using System.Threading;
using Discord;
using Discord.Commands;

namespace Synthbot.DiscordBot
{
	public class DiscordContextAccessor
	{
		private static readonly AsyncLocal<ICommandContext> CommandContextCurrent = new AsyncLocal<ICommandContext>();
		private static readonly AsyncLocal<IUser> UserContextCurrent = new AsyncLocal<IUser>();

		public ICommandContext CommandContext
		{
			get => CommandContextCurrent.Value;
			set => CommandContextCurrent.Value = value;
		}

		public IUser User
		{
			get => CommandContextCurrent.Value?.User ?? UserContextCurrent.Value;
			set => UserContextCurrent.Value = value;
		}

	}

}
