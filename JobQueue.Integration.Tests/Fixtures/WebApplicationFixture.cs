using JobQueue.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace JobQueue.Integration.Tests.Fixtures;

public class WebApplicationFixture(ContainerFixtures containers) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
