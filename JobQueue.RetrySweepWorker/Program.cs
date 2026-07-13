using JobQueue.Core.Interfaces;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.RedisRepository;
using JobQueue.Infrastructure.Repositories;
using JobQueue.RetrySweepWorker;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RetrySweepWorker>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddSingleton<IJobStreamService, JobStreamService>();

var multiplexerOptions = ConfigurationOptions.Parse("localhost:6379");
multiplexerOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(multiplexerOptions));

var host = builder.Build();
host.Run();