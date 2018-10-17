using System;
using System.Linq;
using System.Text;
using AspNetCore.RouteAnalyzer;
using FluentSpotifyApi.AuthorizationFlows.AspNetCore.AuthorizationCode.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Quartz;
using Quartz.DependencyInjection.Microsoft.Extensions;
using SpotifyAPI.Web;
using Synthbot.DAL;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.WebApp.Hubs;
using Synthbot.WebApp.Jobs;
using Synthbot.WebApp.Services;

namespace Synthbot.WebApp
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				// This lambda determines whether user consent for non-essential cookies is needed for a given request.
				options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});
			services.AddLogging();
			services.AddSignalR(opt => { })
				.AddJsonProtocol(jsOpt =>
				{
					jsOpt.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
				});
				//.AddMessagePackProtocol(mpOpt =>
				//{
				//	mpOpt.FormatterResolvers = new List<IFormatterResolver>()
				//	{
				//		MessagePack.Resolvers.StandardResolver.Instance
				//	};
				//});

			services.AddHttpContextAccessor();

			services.AddDbContext<ApplicationDbContext>(options =>
			{
				options.UseSqlServer(
					Configuration.GetConnectionString("DefaultConnection"), opt =>
						opt.MigrationsAssembly("Synthbot.WebApp"));
			});
			services.AddDefaultIdentity<SynthbotUser>(opt =>
				{
					// TODO configure token providers here (if we use it for email verification)
				})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddUserManager<UserManager<SynthbotUser>>()
				;

			services.AddScoped<UserService>();

			services.AddRouteAnalyzer();

			services.AddTransient<DiscordOAuthEvents>();
			services.AddTransient<DiscordBotClientJwtBearerEvents>();
			services.AddAuthentication()
				.AddCookie(
					o =>
					{
						o.LoginPath = new PathString("/account/login");
						o.LogoutPath = new PathString("/account/logout");
					})
				.AddSpotify(
					SpotifyDefaults.AuthenticationScheme,
					o =>
					{
						o.ClientId = Configuration["spotify.api.clientid"];
						o.ClientSecret = Configuration["spotify.api.clientsecret"];
						o.Scope.Add("playlist-read-private");
						o.Scope.Add("playlist-read-collaborative");
						o.Scope.Add("user-read-currently-playing");
						o.Scope.Add("user-read-playback-state");
						o.Scope.Add("user-modify-playback-state");
						o.Scope.Add("user-follow-read");
						o.Scope.Add("user-read-email");
						o.Scope.Add("user-library-read");
						o.Scope.Add("user-top-read");
						o.Scope.Add("playlist-modify-private");
						o.Scope.Add("user-follow-modify");
						o.SaveTokens = true;
						o.EventsType = typeof(DiscordOAuthEvents);
					})
				.AddJwtBearer("discord-bot", opt =>
				{
					opt.TokenValidationParameters =
						new TokenValidationParameters
						{
							LifetimeValidator = (before, expires, token, param) =>
								expires > DateTime.UtcNow && before < DateTime.UtcNow,
							IssuerSigningKey =
								new SymmetricSecurityKey(
									Encoding.ASCII.GetBytes(Configuration["synthbot.token.sharedsecret"])),
							ValidAudiences = new[] {"Synthbot.WebApp"},
							ValidIssuers = new[] {"Synthbot.DiscordBot"},
							ValidateAudience = true,
							ValidateIssuer = true,
							ValidateActor = false,
							ValidateLifetime = true,
						};

					opt.EventsType = typeof(DiscordBotClientJwtBearerEvents);
				});
				// TODO evaluate if Discord login will ever be needed. I think this should be out of scope for initial release
				//.AddDiscord(
				//	//DiscordAuthenticationDefaults.AuthenticationScheme,
				//	opt =>
				//	{
				//		opt.ClientId = Configuration["discord.bot.clientid"];
				//		opt.ClientSecret = Configuration["discord.bot.clientsecret"];
				//		//opt.Scope.Add("identity");
				//		//opt.Scope.Add("email");
				//		opt.SaveTokens = true;
				//		opt.Events.OnRedirectToAuthorizationEndpoint = context =>
				//		{
				//			context.Response.Redirect(context.RedirectUri);
				//			return Task.CompletedTask;
				//		};
				//		opt.Events.OnCreatingTicket = context =>
				//		{
				//			return Task.CompletedTask;
				//		};
				//		opt.Events.OnRemoteFailure = context =>
				//		{
				//			return Task.CompletedTask;
				//		};
				//		opt.Events.OnTicketReceived = context =>
				//		{
				//			return Task.CompletedTask;
				//		};
				//	});


			services.AddSingleton<IScheduler>(
				provider =>
				{
					var scheduler = provider.GetService<ISchedulerFactory>().GetScheduler().Result;
					scheduler.Start().Wait();
					return scheduler;
				});
			services.AddScoped<PlaybackSessionService>();
			services.AddScoped<PlaybackSessionRepository>();
			services.AddScoped<SongPlaybackRepository>();
			services.AddScoped<SpotifyPlaybackService>();
			services.AddScoped<UserTokenService>();
			services.AddHttpClient<SpotifyTokenRefreshService>();
			services.AddSingleton<UserIdCache>();
			services.AddTransient<SongFinishedJob>();
			services.AddSingleton<SpotifyHttpClientFactory>();
			services.AddHttpClient<SpotifyWebClient>();
			services.AddTransient<SpotifyWebAPI>();
			services.AddScoped<DiscordUserRepository>();

			services.AddQuartz();

			services.AddMemoryCache();

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}


		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ApplicationDbContext dbContext, IServiceProvider serviceProvider)
		{
			dbContext.Database.Migrate();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseStaticFiles();
			app.UseCookiePolicy();

			app.UseAuthentication();

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");

				routes.MapRouteAnalyzer("/routes");

			});

			app.UseWebSockets();
			app.UseSignalR(routes =>
			{
				routes.MapHub<DiscordBotHub>("/bot-hub");
			});

			StartRunningPlaybackSessions(dbContext, serviceProvider);

		}

		private void StartRunningPlaybackSessions(ApplicationDbContext dbContext, IServiceProvider serviceProvider)
		{
			// Resume any playback with a song in playing state
			var runningSessions = dbContext.PlaybackSessions
				.Include(s => s.CurrentSongPlayback)
				.Where(s => s.CurrentSongPlayback.State == PlaybackState.Playing && s.JoinedUsers.Any())
				.ToList();

			// Create new jobs to fire on expected completion (or now if expected completion was in the past)
			foreach (var session in runningSessions)
			{
				// Spawn next job
				var jobId = $"SongFinishedJob-{Guid.NewGuid().ToString()}";

				// Update song tracker with new job id
				dbContext.SongPlaybackTrackers.Attach(session.CurrentSongPlayback);
				session.CurrentSongPlayback.JobId = jobId;
				dbContext.SaveChanges();

				// Create and schedule job
				var msUntilFinished = DateTime.UtcNow > session.CurrentSongPlayback.ExpectedFinishUtc
					? 0
					: (session.CurrentSongPlayback.ExpectedFinishUtc - DateTime.UtcNow).TotalMilliseconds;

				var job = JobBuilder.Create<SongFinishedJob>()
					.WithIdentity(jobId)
					.WithDescription($"SongFinishedJob SessionId: {session.Id}, PlaylistId: {session.SpotifyPlaylistId}, SongUri: {session.CurrentSongPlayback.SpotifySongUri}")
					.UsingJobData("PlaybackSessionId", session.Id)
					.UsingJobData("SpotifySongUri", session.CurrentSongPlayback.SpotifySongUri)
					.Build();
				var trigger = TriggerBuilder.Create()
					.ForJob(job)
					.StartAt(DateTimeOffset.UtcNow.AddMilliseconds(msUntilFinished))
					.Build();

				using (var scope = serviceProvider.CreateScope())
				{
					var jobScheduler = scope.ServiceProvider.GetService<IScheduler>();
					jobScheduler.ScheduleJob(job, trigger).Wait();
				}
			}
		}
	}
}
