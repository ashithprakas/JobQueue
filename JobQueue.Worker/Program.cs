using JobQueue.Application.Services;
using JobQueue.Core.Interfaces;
using JobQueue.Infrastructure.Repositories;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.Messaging;
using JobQueue.Worker;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHostedService<Worker>();
var multiplexerOptions = ConfigurationOptions.Parse("localhost:6379");
multiplexerOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(multiplexerOptions));
builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();
var host = builder.Build();
host.Run();