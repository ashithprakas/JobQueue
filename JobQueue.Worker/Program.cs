using JobQueue.application.Services;
using JobQueue.Core.interfaces;
using JobQueue.infrastructure.Repositories;
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
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();
var host = builder.Build();
host.Run();