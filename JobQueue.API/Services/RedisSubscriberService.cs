using JobQueue.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace JobQueue.API.Services;

public class RedisSubscriberService(IConnectionMultiplexer redis,IHubContext<JobStatusHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync("job-status", async (channel, message) =>
        {
            var parts = message.ToString().Split(':');
            if (parts.Length == 2)
            {
                var jobId = parts[0];
                var status = parts[1];

                await hubContext.Clients.All.SendAsync("JobStatusChanged", jobId, status);
            }
        });
            await Task.Delay(Timeout.Infinite,stoppingToken);
    }
}