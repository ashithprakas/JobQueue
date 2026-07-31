using System.Diagnostics;
using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using JobQueue.Core.Telemetry.Config;

namespace JobQueue.Worker;

public class Worker(IServiceScopeFactory scopeFactory, IJobStreamService jobStreamService, ILogger<Worker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobStreamService.EnsureConsumerGroupAsync();
        var consumerName = Environment.MachineName + "_" + Guid.NewGuid();
        List<StreamJobEntry> jobStream;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = DiagnosticConfig.ActivitySource.StartActivity("ProcessJobBatch");
            using var scope = scopeFactory.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
            try
            {
                jobStream = await jobStreamService.ReadJobsAsync(consumerName, JobConstants.JobProcessCount);
                activity?.AddTag("JobProcessCount", jobStream.Count);
            }
            catch (Exception ex)
            {
                activity?.AddTag("JobProcessError", ex.Message);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex, "Failed to read from job stream");
                await Task.Delay(5000, stoppingToken);
                continue;
            }

            foreach (var job in jobStream)
            {
                using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = job.JobId }))
                {
                    logger.LogInformation("Processing Job");

                    ActivityContext parentContext = default;
                    var hasParent = !string.IsNullOrEmpty(job.TraceId) &&
                                    ActivityContext.TryParse(job.TraceId, null, out parentContext);
                    using var jobActivity = hasParent
                        ? DiagnosticConfig.ActivitySource.StartActivity("ProcessJob", ActivityKind.Consumer,
                            parentContext)
                        : DiagnosticConfig.ActivitySource.StartActivity("ProcessJob");
                    jobActivity?.AddTag("JobId", job.JobId.ToString());

                    try
                    {
                        await jobService.ProcessJob(job.JobId);
                        await jobStreamService.AcknowledgeAsync(job.EntryId);
                        jobActivity?.AddTag("AcknowledgedEntryId", job.EntryId);
                    }
                    catch (Exception ex)
                    {
                        jobActivity?.AddTag("JobError", ex.Message);
                        jobActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        logger.LogError(ex, "Error processing job");
                    }
                }
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
