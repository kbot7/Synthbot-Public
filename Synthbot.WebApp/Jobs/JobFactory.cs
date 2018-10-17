using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Synthbot.WebApp.Jobs
{
	public class JobFactory : IJobFactory
	{
		private readonly IServiceScopeFactory _serviceScopeFactory;

		public JobFactory(IServiceScopeFactory serviceScopeFactory)
		{
			// TODO this scope will last the lifetime of the application, since the JobFactory is a singleton. We likely need a better way to manage object lifecycles
			_serviceScopeFactory = serviceScopeFactory;
		}

		public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
		{
			var scope = _serviceScopeFactory.CreateScope();
			var job = scope.ServiceProvider.GetService(bundle.JobDetail.JobType) as IJob;
			return job;
		}

		public void ReturnJob(IJob job)
		{
			var disposable = job as IDisposable;
			disposable?.Dispose();
		}
	}
}
