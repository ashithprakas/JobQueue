using JobQueue.API.DTOs;
using JobQueue.Core.Interfaces;

namespace JobQueue.API.Endpoints;

public static class JobEndpoints
{
     public static void MapJobEndpoints(this WebApplication app)
    {
        app.MapGet("/health", ()=>Results.Ok("Healthy"));
        app.MapPost("/jobs", async (CreateJobRequest createJobRequest, IJobService jobService) =>
        {
            var job = await jobService.CreateJob(createJobRequest.Payload);
            return Results.Created($"/jobs/{job.Id}", job);
        });
        app.MapGet("/jobs/{id}/status", async (Guid id, IJobService jobService) =>
        {
            var status =  await jobService.GetJobStatus(id);
            return Results.Ok(status);
        });
    }
}