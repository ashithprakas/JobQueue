using JobQueue.application.Services;
using JobQueue.Core.interfaces;
using JobQueue.infrastructure.Repositories;
using JobQueue.Infrastructure;
using JobQueue.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();