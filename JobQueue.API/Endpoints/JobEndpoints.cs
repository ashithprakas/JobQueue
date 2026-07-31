using System.Diagnostics;
using FluentValidation;
using JobQueue.API.DTOs;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Telemetry.Config;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobQueue.API.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var healthCheckEndpointGroup = app.MapGroup("/health").WithTags("Health Checks");
        healthCheckEndpointGroup.MapGet("/live", (ILogger<Program> logger) =>
        {
            var response = new GenericResponse() { Status = "Healthy" };
            logger.LogInformation(response.ToString());
            return Task.FromResult(Results.Ok(response));
        });
        healthCheckEndpointGroup.MapGet("/ready",
            async (HealthCheckService healthCheckService, ILogger<Program> logger) =>
            {
                var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));

                var response = new GenericResponse()
                {
                    Status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
                };

                if (report.Status != HealthStatus.Healthy)
                {
                    logger.LogWarning("Readiness check failed: {Report}", report.Entries);
                }

                return report.Status == HealthStatus.Healthy
                    ? Results.Ok(response)
                    : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
            });

        app.MapPost("/jobs",
            async (CreateJobRequest createJobRequest, IJobService jobService, IValidator<CreateJobRequest> validator,
                ILogger<Program> logger) =>
            {
                using (logger.BeginScope(new Dictionary<string, object>
                { ["CorrelationId"] = createJobRequest.Id.ToString() }))
                {
                    using var activity = DiagnosticConfig.ActivitySource.StartActivity("CreateJob");
                    activity?.AddTag("JobId", createJobRequest.Id.ToString());

                    var validationResult = await validator.ValidateAsync(createJobRequest);
                    if (!validationResult.IsValid)
                    {
                        activity?.AddTag("Outcome", "ValidationFailed");
                        logger.LogInformation("Validation Failed : {Errors}", validationResult.Errors);
                        return Results.ValidationProblem(validationResult.ToDictionary());
                    }

                    try
                    {
                        // Payload! is safe: the validation-failed branch above already returned for a null/empty Payload
                        var job = await jobService.CreateJob(createJobRequest.Id, createJobRequest.Payload!);
                        activity?.AddTag("Outcome", "Created");
                        return Results.Created($"/jobs/{job.Id}", job);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
                    {
                        var id = createJobRequest.Id;
                        var job = await jobService.GetJobById(id);
                        activity?.AddTag("Outcome", "Conflict");
                        logger.LogError(ex, "Job Id Already Exists");
                        return Results.Conflict(job);
                    }
                    catch (Exception ex)
                    {
                        activity?.AddTag("Outcome", "Error");
                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        logger.LogError(ex, "Error : ");
                        return Results.Problem(ex.Message);
                    }
                }
            });
        app.MapGet("/jobs/{id}/status", async (Guid id, IJobService jobService, ILogger<Program> logger) =>
        {
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id.ToString() }))
            {
                using var activity = DiagnosticConfig.ActivitySource.StartActivity("GetJobStatus");
                activity?.AddTag("JobId", id.ToString());
                var status = await jobService.GetJobStatus(id);
                activity?.AddTag("JobStatus", status.ToString());
                logger.LogInformation("Returning Job Status as {status} ", status);
                return Results.Ok(status);
            }
        });
    }
}
