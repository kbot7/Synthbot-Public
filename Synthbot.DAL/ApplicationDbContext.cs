using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synthbot.DAL.Models;

namespace Synthbot.DAL
{
	public class ApplicationDbContext : IdentityDbContext<SynthbotUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
			
		}

		public DbSet<ReferralTokenReceipt> ReferralTokenReceipts { get; set; }
		public DbSet<PlaybackSession> PlaybackSessions { get; set; }
		public DbSet<SongPlaybackTracker> SongPlaybackTrackers { get; set; }
		public DbSet<DiscordUser> DiscordUsers { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Don't create the Role tables. We are using claims-based auth, and don't need them
			modelBuilder.Ignore<IdentityRole>();
			modelBuilder.Ignore<IdentityUserRole<string>>();
			modelBuilder.Ignore<IdentityRoleClaim<string>>();

			// Map ASP.NET Identities to custom table names
			modelBuilder.Entity<SynthbotUser>().ToTable("SynthbotUsers");
			modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("SynthbotUserClaims");
			modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("SynthbotUserLoginProviders");
			modelBuilder.Entity<IdentityUserToken<string>>().ToTable("SynthbotUserTokens");

			modelBuilder.Entity<SongPlaybackTracker>()
				.HasOne(e => e.PlaybackSession)
				.WithMany(e => e.SongPlaybacks);

			modelBuilder.Entity<SongPlaybackTracker>()
				.Property(e => e.Id)
				.HasDefaultValueSql("NEWID()");

			modelBuilder.Entity<SongPlaybackTracker>()
				.Property(e => e.State)
				.HasConversion<string>();

			// PlaybackSession Configuration
			modelBuilder.Entity<PlaybackSession>()
				.Property(e => e.Id)
				.HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<PlaybackSession>()
				.HasMany(e => e.JoinedUsers)
				.WithOne(e => e.ActivePlaybackSession);
			modelBuilder.Entity<PlaybackSession>()
				.HasMany(e => e.SongPlaybacks)
				.WithOne(e => e.PlaybackSession);
			modelBuilder.Entity<PlaybackSession>()
				.HasAlternateKey(c => c.DiscordVoiceChannelId);
			modelBuilder.Entity<PlaybackSession>()
				.HasIndex(c => c.DiscordVoiceChannelId)
				.IsUnique();

			modelBuilder.Entity<DiscordUser>()
				.HasIndex(e => e.DiscordUserId)
				.IsUnique();

			modelBuilder.Entity<DiscordUser>()
				.HasIndex(e => e.DiscordUsername);

			modelBuilder.Entity<DiscordUser>()
				.Property(e => e.UserStatus)
				.HasConversion<string>();

			modelBuilder.Entity<SynthbotUser>()
				.HasOne(e => e.DiscordUser)
				.WithOne(e => e.SynthbotUser);

			modelBuilder.Entity<DiscordUser>().Property(e => e.UserStatus)
				.HasDefaultValue(DiscordUserStatus.NoResponse);
		}
	}
}
