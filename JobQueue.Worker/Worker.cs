using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using StackExchange.Redis;

namespace JobQueue.Worker;

public class Worker(IServiceScopeFactory scopeFactory, IJobStreamService jobStreamService) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobStreamService.EnsureConsumerGroupAsync();
        var consumerName = Environment.MachineName + "_" + Guid.NewGuid();
        List<StreamJobEntry> jobStream;
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
            try
            {
                jobStream = await jobStreamService.ReadJobsAsync(consumerName, JobConstants.JobProcessCount);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to read from job stream, Will restart in 5 seconds : "+ex.Message);
                await Task.Delay(5000, stoppingToken);
                continue;
            }
            foreach (var job in jobStream)
            {
                await jobService.ProcessJob(job.JobId);
                await jobStreamService.AcknowledgeAsync(job.EntryId);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}