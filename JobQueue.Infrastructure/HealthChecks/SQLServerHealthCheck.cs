using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobQueue.Infrastructure.HealthChecks;

public class SqlServerHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("HealthCheckConnection");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await connection.OpenAsync(linkedCts.Token);

            return HealthCheckResult.Healthy("SQL Server is running");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server did not respond", ex);
        }
    }
}

