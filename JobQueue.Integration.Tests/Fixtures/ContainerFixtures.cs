using JobQueue.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.Integration.Tests.Fixtures;

using Testcontainers.MsSql;
using Testcontainers.Redis;

public class ContainerFixtures : IAsyncLifetime
{
    public MsSqlContainer SqlServerContainer { get; } =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();

    public RedisContainer RedisContainer { get; } =
        new RedisBuilder("redis:7.0").Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            SqlServerContainer.StartAsync(), RedisContainer.StartAsync());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(SqlServerContainer.GetConnectionString()).Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(SqlServerContainer.DisposeAsync().AsTask(), RedisContainer.DisposeAsync().AsTask());
    }
}
