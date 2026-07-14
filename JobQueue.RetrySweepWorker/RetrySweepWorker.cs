using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;

namespace JobQueue.RetrySweepWorker;

public class RetrySweepWorker(IServiceScopeFactory scopeFactory,IJobStreamService jobStreamService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

            var eligibleJobs = await jobRepository.GetJobsToRetryAsync(JobConstants.JobProcessCount);

            foreach (var job in eligibleJobs)
            {
                await jobStreamService.AddJobToQueueAsync(job.Id.ToString());
            }
            
            await Task.Delay(5000, stoppingToken);
        }
    }
}