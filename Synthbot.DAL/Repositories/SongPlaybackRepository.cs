using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synthbot.DAL.Models;

namespace Synthbot.DAL.Repositories
{
	public class SongPlaybackRepository
	{
		private readonly ApplicationDbContext _db;
		private readonly SemaphoreSlim _dbSignal;
		public SongPlaybackRepository(
			ILogger<SongPlaybackRepository> logger,
			ApplicationDbContext db)
		{
			_db = db;
			_dbSignal = new SemaphoreSlim(1, 1);
		}

		public Task<SongPlaybackTracker> GetById(string id)
		{
			return _db.SongPlaybackTrackers.FindAsync(id);
		}

		public async Task Upsert(SongPlaybackTracker playbackTacker)
		{
			await _dbSignal.WaitAsync();

			try
			{
				var entity = await _db.SongPlaybackTrackers.FirstOrDefaultAsync(e => e.Id == playbackTacker.Id);
				if (entity != null)
				{
					_db.SongPlaybackTrackers.Attach(entity);
					entity = playbackTacker;
				}
				else
				{
					await _db.SongPlaybackTrackers.AddAsync(playbackTacker);
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
