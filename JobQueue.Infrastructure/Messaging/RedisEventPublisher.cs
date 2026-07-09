using JobQueue.Core.Enums;
using JobQueue.Core.Interfaces;
using StackExchange.Redis;

namespace JobQueue.Infrastructure.Messaging;

public class RedisEventPublisher(IConnectionMultiplexer redis) : IEventPublisher
{
    public async Task PublishJobStatusUpdate(Guid jobId, JobStatus status)
    {
        var subscriber = redis.GetSubscriber();
        var message = $"{jobId}:{status}";
        await subscriber.PublishAsync("job-status", message);
    }
}