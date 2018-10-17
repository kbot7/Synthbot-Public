using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Synthbot.WebApp.Areas.Identity.IdentityHostingStartup))]
namespace Synthbot.WebApp.Areas.Identity
{
	public class IdentityHostingStartup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) => {
			});
		}
	}
}
