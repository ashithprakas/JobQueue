using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;

namespace JobQueue.RetrySweepWorker;

public class RetrySweepWorker(IServiceScopeFactory scopeFactory,IJobStreamService jobStreamService,ILogger<RetrySweepWorker>logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured during job execution");
            }
        }
    }
}