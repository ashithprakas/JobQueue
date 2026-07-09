using JobQueue.Application.Services;
using JobQueue.Core.Interfaces;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using JobQueue.API.Endpoints;
using System.Text.Json.Serialization;
using JobQueue.API.Services;
using JobQueue.Core.Exceptions;
using JobQueue.Infrastructure.Messaging;
using Microsoft.AspNetCore.Diagnostics;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();
builder.Services.AddHostedService<RedisSubscriberService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("null", "http://localhost", "http://127.0.0.1:5500")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (statusCode, message) = exception switch
        {
            NotFoundException ex => (404, ex.Message),
            _ => (500, "An unexpected error occurred")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});
app.UseHttpsRedirection();
app.MapJobEndpoints();
app.UseCors();
app.MapSignalREndpoints();
app.Run();


