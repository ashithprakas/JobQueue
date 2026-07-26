using JobQueue.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace JobQueue.Integration.Tests.Fixtures;

public class WorkerHostFixture(ContainerFixtures fixtures):IAsyncLifetime
{
    private IHost? _host;

    public async ValueTask DisposeAsync()
    {
       if(_host is not null){
           await _host.StopAsync();
           _host.Dispose();
       }
    }

    public async ValueTask InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        WorkerServiceRegistration.ConfigureServices(builder);

        builder.Services.RemoveAll<DbContextOptions<AppDbContext>>();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(fixtures.SqlServerContainer.GetConnectionString()));

        builder.Services.RemoveAll<IConnectionMultiplexer>();
        builder.Services.AddSingleton<IConnectionMultiplexer>(_=>ConnectionMultiplexer.Connect(fixtures.RedisContainer.GetConnectionString()));
        
        _host = builder.Build();
        await _host.StartAsync();

    }
}