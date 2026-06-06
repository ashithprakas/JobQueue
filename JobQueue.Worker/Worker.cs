using JobQueue.Core.interfaces;

namespace JobQueue.Worker;

public class Worker(IServiceScopeFactory scopeFactory) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Worker running at:"+ DateTimeOffset.Now);
            using var scope = scopeFactory.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
            var jobList = await jobService.GetPendingJobs();
            
            foreach (var job in jobList)
            {
                Console.WriteLine("ExecutingJob: " + job.Id);
                await jobService.ProcessJob(job.Id);
                
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}