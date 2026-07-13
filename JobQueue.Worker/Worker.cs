using JobQueue.Core.Interfaces;

namespace JobQueue.Worker;

public class Worker(IServiceScopeFactory scopeFactory, IJobStreamService jobStreamService) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobStreamService.EnsureConsumerGroupAsync();
        var consumerName = Environment.MachineName + "_" + Guid.NewGuid();
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
            var jobList = await jobStreamService.ReadJobsAsync(consumerName, 5);
            foreach (var job in jobList)
            {
                await jobService.ProcessJob(job.JobId);
                await jobStreamService.AcknowledgeAsync(job.EntryId);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}