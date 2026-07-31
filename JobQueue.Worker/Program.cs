using JobQueue.Application.Services;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Telemetry.Config;
using JobQueue.Infrastructure.Repositories;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.Messaging;
using JobQueue.Infrastructure.RedisRepository;
using JobQueue.Worker;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext().WriteTo
    .Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/worker-logs.txt",
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
WorkerServiceRegistration.ConfigureServices(builder);
var host = builder.Build();
host.Run();

public static class WorkerServiceRegistration
{
    public static void ConfigureServices(HostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddScoped<IJobRepository, JobRepository>();
        builder.Services.AddScoped<IJobService, JobService>();
        builder.Services.AddHostedService<Worker>();
        var multiplexerOptions = ConfigurationOptions.Parse("localhost:6379");
        multiplexerOptions.AbortOnConnectFail = false;
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(multiplexerOptions));
        builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();
        builder.Services.AddSingleton<IJobStreamService, JobStreamService>();
        builder.Services.AddSerilog();

        const string serviceName = "JobQueue.Worker";
        const string serviceVersion = "v1";
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddSource(DiagnosticConfig.ActivitySource.Name)
                .AddOtlpExporter(options => { options.Endpoint = new Uri("http://localhost:4317"); }))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(options => { options.Endpoint = new Uri("http://localhost:4317"); }));
    }
}
