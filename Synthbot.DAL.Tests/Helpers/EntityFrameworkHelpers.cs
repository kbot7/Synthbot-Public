using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Synthbot.DAL.Tests.Helpers
{
	public static class EntityFrameworkHelpers
	{
		public static DbContextOptions<ApplicationDbContext> InMemoryOptions() => new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		public static DbContextOptions<ApplicationDbContext> SqlDb()
		{
			var efOpts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database=Synthbot.WebApp.Identity.Dev.UnitTest.{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true", opt =>
			{

			}).Options;

			return efOpts;
		}

		public static async Task<TestContext> SqlContextAsync()
		{
			var context = new TestContext(SqlDb());

			await context.Database.EnsureDeletedAsync();
			await context.Database.EnsureCreatedAsync();
			await context.Database.MigrateAsync();

			return context;
		}
	}

	public class TestContext : ApplicationDbContext, IDisposable
	{
		public TestContext(DbContextOptions<ApplicationDbContext> opt) : base(opt)
		{

		}

		public override void Dispose()
		{
			this.Database.EnsureDeleted();
			base.Dispose();
		}
	}
}
