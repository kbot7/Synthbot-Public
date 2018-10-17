using System;
using Xunit;

namespace Synthbot.DiscordBot.UnitTests
{
	public class EmbedBuilderTests
	{
		[Fact]
		public void DateTimeFormat()
		{
			var timespan = TimeSpan.FromMinutes(2.25);

			var formatted = $"{timespan:mm\\:ss}";
			
		}
	}
}
