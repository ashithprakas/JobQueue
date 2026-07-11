using JobQueue.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace JobQueue.API.Services;

public class RedisSubscriberService(IConnectionMultiplexer redis,IHubContext<JobStatusHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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
                Console.WriteLine("Connected to Redis");
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error subscribing to redis :" + e.Message);
                Console.WriteLine("Retrying in 5 seconds...");
                await Task.Delay(500, stoppingToken);
            }

        }
        await Task.Delay(Timeout.Infinite,stoppingToken);
    }
}