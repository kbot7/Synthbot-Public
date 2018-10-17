using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synthbot.DAL.Models;

namespace Synthbot.DAL.Repositories
{
	public class DiscordUserRepository
	{
		private readonly ApplicationDbContext _db;
		private readonly SemaphoreSlim _dbSignal;
		public DiscordUserRepository(ApplicationDbContext db)
		{
			_db = db;
			_dbSignal = new SemaphoreSlim(1, 1);
		}

		public Task<DiscordUser> GetById(string discordUserId)
		{
			return _db.DiscordUsers.FindAsync(discordUserId);
		}

		public async Task Upsert(DiscordUser user)
		{
			await _dbSignal.WaitAsync();

			try
			{
				var entity = await _db.DiscordUsers.FirstOrDefaultAsync(e => e.DiscordUserId == user.DiscordUserId);
				if (entity != null)
				{
					_db.DiscordUsers.Update(user);
				}
				else
				{
					await _db.DiscordUsers.AddAsync(user);
				}
				await _db.SaveChangesAsync();
			}
			finally
			{
				_dbSignal.Release();
			}
		}
	}
}
