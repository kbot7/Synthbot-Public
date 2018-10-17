using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Synthbot.DiscordBot.Modules
{
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly CommandService _service;
		//private readonly IConfigurationRoot _config;

		public HelpModule(CommandService service)
		{
			_service = service;
			//_config = config;
		}

		[Command("help")]
		[Alias("commands")]
		[Summary("List all available commands")]
		public async Task HelpAsync()
		{
			var builder = new EmbedBuilder()
			{
				Color = new Color(114, 137, 218),
				Description = "These are the commands you can use"
			};

			var commands = _service.Modules.SelectMany(m => m.Commands);

			foreach (var cmd in commands)
			{
				var result = await cmd.CheckPreconditionsAsync(Context);
				if (result.IsSuccess)
				{
					var name = cmd.Aliases.First();
					var description = string.IsNullOrWhiteSpace(cmd.Summary) ? "-" : $"- {cmd.Summary}";

					var parameters = cmd.Parameters
						.Where(p => !p.IsOptional)
						.Select(p => $"{{{p.Name}}}");

					builder.AddField(x =>
					{
						x.Name = $"{name} {string.Join(' ', parameters)}";
						x.Value = description;
						x.IsInline = false;
					});
				}

			}

			await ReplyAsync("", false, builder.Build());
		}

		[Command("help")]
		[Summary("Show help for a command. Lists name, description, and parameters")]
		public async Task HelpAsync(string command)
		{
			var result = _service.Search(Context, command);

			if (!result.IsSuccess)
			{
				await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
				return;
			}

			var builder = new EmbedBuilder()
			{
				Color = new Color(114, 137, 218),
				Description = $"Here are some commands like **{command}**"
			};

			foreach (var match in result.Commands)
			{
				var cmd = match.Command;

				var paramList = cmd.Parameters.Where(p => p.Name != "overrideChannelName");

				var paramSection = new StringBuilder("**Parameters**:");
				bool hasParams = false;
				foreach (var parameter in paramList)
				{
					hasParams = true;
					paramSection.Append($"\n{parameter.Name}");
					if (!string.IsNullOrWhiteSpace(parameter.Summary))
					{
						paramSection.Append($"\n - {parameter.Summary}");
					}
				}

				var paramSectionString = paramSection.ToString();

				var fieldValue = hasParams
					? $"{cmd.Summary}\n{paramSectionString}"
					: $"{cmd.Summary}";

				builder.AddField(x =>
				{
					x.Name = string.Join(", ", cmd.Aliases);
					x.Value = $"{fieldValue} {string.Join(' ', paramList.Select(p => $"{{{p.Name}}}"))}";
					x.IsInline = false;
				});
			}

			await ReplyAsync("", false, builder.Build());
		}
	}
}
