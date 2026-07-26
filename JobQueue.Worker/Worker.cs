using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using StackExchange.Redis;

namespace JobQueue.Worker;

public class Worker(IServiceScopeFactory scopeFactory, IJobStreamService jobStreamService, ILogger<Worker> logger) : BackgroundService
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
                logger.LogError(ex, "Failed to read from job stream");
                await Task.Delay(5000, stoppingToken);
                continue;
            }
            
            foreach (var job in jobStream)
            {
                using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = job.JobId }))
                {
                    logger.LogInformation("Processing Job");
                    
                    try
                    {
                        await jobService.ProcessJob(job.JobId);
                        await jobStreamService.AcknowledgeAsync(job.EntryId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing job");
                    }
                }
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
