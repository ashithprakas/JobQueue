using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace JobQueue.Infrastructure.HealthChecks;

public class RedisServerHealthChecks(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var pingTask = redis.GetDatabase().PingAsync();
            var completedTask = await Task.WhenAny(pingTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask != pingTask)
            {
                return HealthCheckResult.Unhealthy("Redis Server did not respond");
            }

            return HealthCheckResult.Healthy("Redis Server is running");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis Server threw", ex);
        }
    }
}
