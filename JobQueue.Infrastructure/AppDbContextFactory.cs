using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace JobQueue.Infrastructure;

// Design-time-only factory. EF Core's tooling (dotnet ef migrations add / database update)
// looks for a class like this before it tries to boot the whole app (Program.cs, including
// Redis) just to find the DbContext. This gives it a direct, minimal path instead — so
// generating a migration doesn't require Redis (or anything else unrelated) to be running.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("2f623ae1-a929-4cdc-9c75-6959d4628912")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
