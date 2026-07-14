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
        app.MapGet("/health", ()=>Results.Ok("Healthy"));
        app.MapPost("/jobs", async (CreateJobRequest createJobRequest, IJobService jobService,IValidator<CreateJobRequest> validator) =>
        {
            var validationResult = await validator.ValidateAsync(createJobRequest);
            if (!validationResult.IsValid)
            {
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
                return Results.Conflict(job);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }

        });
        app.MapGet("/jobs/{id}/status", async (Guid id, IJobService jobService) =>
        {
            var status =  await jobService.GetJobStatus(id);
            return Results.Ok(status);
        });
    }
}