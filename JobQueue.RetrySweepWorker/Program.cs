using JobQueue.Core.Interfaces;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.RedisRepository;
using JobQueue.Infrastructure.Repositories;
using JobQueue.RetrySweepWorker;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning).Enrich.FromLogContext().WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}").WriteTo.File("Logs/sweeper-logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}").CreateLogger();
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

var host = builder.Build();
host.Run();
