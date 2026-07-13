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
        // Same UserSecretsId already sitting in JobQueue.API.csproj — this points at that
        // exact same local secrets file rather than creating a separate one. The real
        // connection string still never touches source control; only this GUID (which is
        // just a pointer to a local file path, not a secret itself) is in the code.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "JobQueue.API"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets("2f623ae1-a929-4cdc-9c75-6959d4628912")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
