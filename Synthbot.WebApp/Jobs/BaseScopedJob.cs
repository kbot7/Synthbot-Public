using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Synthbot.WebApp.Jobs
{
	public abstract class BaseScopedJob<T> : IJob
	{
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly Type _jobType;

		public BaseScopedJob(T jobType, IServiceScopeFactory serviceScopeFactory)
		{
			_serviceScopeFactory = serviceScopeFactory;
			_jobType = typeof(T);
		}

		public async Task Execute(IJobExecutionContext context)
		{
			using (var scope = _serviceScopeFactory.CreateScope())
			{
				try
				{
					if (scope.ServiceProvider.GetService(_jobType) is IJob currentJob)
					{
						await currentJob.Execute(context);
					}
				}
				catch (JobExecutionException)
				{
					throw;
				}
				catch (Exception e)
				{
					throw new JobExecutionException($"Failed to exexcute job '{context.JobDetail.Key}' of type '{context.JobDetail.JobType}", e);
				}
			}
		}
	}
}
