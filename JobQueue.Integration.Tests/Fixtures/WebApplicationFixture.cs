using JobQueue.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace JobQueue.Integration.Tests.Fixtures;

public class WebApplicationFixture(ContainerFixtures containers) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // SqlServerHealthCheck reads ConnectionStrings:HealthCheckConnection directly off
            // IConfiguration (not the EF DbContext), so overriding DbContextOptions below doesn't
            // reach it. Without this, /health/ready dials whatever's in user-secrets/appsettings
            // (a real local SQL Server) instead of this test run's ephemeral container, and the
            // readiness check comes back Unhealthy even though the container is fine.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HealthCheckConnection"] = containers.SqlServerContainer.GetConnectionString(),
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(containers.SqlServerContainer.GetConnectionString()));
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(containers.RedisContainer.GetConnectionString()));
        });
    }
}
