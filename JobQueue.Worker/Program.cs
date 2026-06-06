using JobQueue.API.Services;
using JobQueue.application.Services;
using JobQueue.Core.interfaces;
using JobQueue.infrastructure.Repositories;
using JobQueue.Infrastructure;
using JobQueue.Worker;
using JobQueue.Worker.Service;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IJobNotificationService, NoOpJobNotificationSerivce>();
var host = builder.Build();
host.Run();