using FluentValidation;
using FluentValidation.Results;
using JobQueue.API.DTOs;
using JobQueue.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.API.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        app.MapGet("/health", Task<IResult> (ILogger<Program> logger) =>
        {
            var response = new GenericResponse() { Status = "Healthy" };
            logger.LogInformation(response.ToString());
            return Task.FromResult(Results.Ok(response));
        });

        app.MapPost("/jobs",
            async (CreateJobRequest createJobRequest, IJobService jobService, IValidator<CreateJobRequest> validator,
                ILogger<Program> logger) =>
            {
                using (logger.BeginScope(new Dictionary<string, object>
                { ["CorrelationId"] = createJobRequest.Id.ToString() }))
                {
                    var validationResult = await validator.ValidateAsync(createJobRequest);
                    if (!validationResult.IsValid)
                    {
                        logger.LogInformation("Validation Failed : {Errors}", validationResult.Errors);
                        return Results.ValidationProblem(validationResult.ToDictionary());
                    }

                    try
                    {
                        var job = await jobService.CreateJob(createJobRequest.Id, createJobRequest.Payload);
                        return Results.Created($"/jobs/{job.Id}", job);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
                    {
                        var id = createJobRequest.Id;
                        var job = await jobService.GetJobById(id);
                        logger.LogError(ex, "Job Id Already Exists");
                        return Results.Conflict(job);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error : ");
                        return Results.Problem(ex.Message);
                    }
                }
            });
        app.MapGet("/jobs/{id}/status", async (Guid id, IJobService jobService, ILogger<Program> logger) =>
        {
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id.ToString() }))
            {
                var status = await jobService.GetJobStatus(id);
                logger.LogInformation("Returning Job Status as {status} ", status);
                return Results.Ok(status);
            }
        });
    }
}