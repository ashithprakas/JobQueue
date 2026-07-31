using System.Diagnostics;
using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Telemetry.Config;

namespace JobQueue.RetrySweepWorker;

public class RetrySweepWorker(
    IServiceScopeFactory scopeFactory,
    IJobStreamService jobStreamService,
    ILogger<RetrySweepWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = DiagnosticConfig.ActivitySource.StartActivity("ProcessRetryJobs");
            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

                var eligibleJobs = await jobRepository.GetJobsToRetryAsync(JobConstants.JobProcessCount);
                activity?.SetTag("RetryJobCount", eligibleJobs.Count);
                foreach (var job in eligibleJobs)
                {
                    await jobStreamService.AddJobToQueueAsync(job.Id.ToString());
                    activity?.AddTag("JobIdAddedToQueue", job.Id);
                }

                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                activity?.AddTag("JobError", ex.Message);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex, "An error occured during job execution");
            }
        }
    }
}
