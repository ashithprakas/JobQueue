using JobQueue.Application.Services;
using JobQueue.Core.Interfaces;
using JobQueue.Infrastructure;
using JobQueue.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using JobQueue.API.Endpoints;
using System.Text.Json.Serialization;
using FluentValidation;
using JobQueue.API.DTOs;
using JobQueue.API.Services;
using JobQueue.Core.Exceptions;
using JobQueue.Infrastructure.Messaging;
using JobQueue.Infrastructure.RedisRepository;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/api-logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

var multiplexerOptions = ConfigurationOptions.Parse("localhost:6379");
multiplexerOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(multiplexerOptions));
builder.Services.AddSingleton<IJobStreamService, JobStreamService>();
builder.Services.AddSingleton<IEventPublisher, RedisEventPublisher>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateJobRequestValidator>();
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
builder.Services.AddSerilog();

var app = builder.Build();

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

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.MapJobEndpoints();

app.UseCors();
app.MapSignalREndpoints();

app.Run();

public partial class Program {}
