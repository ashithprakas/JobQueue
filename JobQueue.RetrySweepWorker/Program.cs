using JobQueue.Core.Interfaces;
using JobQueue.Core.Telemetry.Config;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.RedisRepository;
using JobQueue.Infrastructure.Repositories;
using JobQueue.RetrySweepWorker;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo
    .Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo
    .File("Logs/sweeper-logs.txt",
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RetrySweepWorker>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddSingleton<IJobStreamService, JobStreamService>();

var multiplexerOptions = ConfigurationOptions.Parse("localhost:6379");
multiplexerOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(multiplexerOptions));
builder.Services.AddSerilog();
const string serviceName = "JobQueue.RetrySweepWorker";
const string serviceVersion = "v1";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddSource(DiagnosticConfig.ActivitySource.Name)
        .AddOtlpExporter(options => { options.Endpoint = new Uri("http://localhost:4317"); })
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(options => { options.Endpoint = new Uri("http://localhost:4317"); }));

var host = builder.Build();
host.Run();
