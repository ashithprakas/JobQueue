using JobQueue.Core.Constants;
using JobQueue.Core.Enums;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using JobQueue.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace JobQueue.Application.Services;

public class JobService(IJobRepository jobRepository, IEventPublisher eventPublisher, IJobStreamService redisRepository, ILogger<IJobService> logger) : IJobService
{
    public async Task<Job> CreateJob(Guid Id, string payload)
    {
        var job = new Job()
        {
            Id = Id,
            Payload = payload,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Attempts = 0
        };

        await jobRepository.AddAsync(job);
        try
        {
            await redisRepository.AddJobToQueueAsync(job.Id.ToString());
        }
        catch (Exception e)
        {
            job.RetryAt = DateTime.UtcNow + new TimeSpan(0, 5, Random.Shared.Next(0, 60));
            job.ErrorMessage = e.Message;

            try
            {
                await jobRepository.UpdateAsync(job);
            }
            catch (Exception dbEx)
            {
                // Could not persist the failure itself (e.g. SQL Server unreachable). The job is
                // now stuck at Status = Pending with no recorded error and no RetryAt saved —
                // GetPendingJobsAsync only fetches Status == Pending, so it will never be picked
                // up again on its own. Logged loudly on purpose: this means a job is lost, not
                // just "an error happened." Full recovery for this case is out of scope for now
                // (see Task 18 — outbox pattern / dead-letter reprocessing).
                logger.LogCritical(dbEx, "Failed to persist failure state for job {JobId} — job is now stuck at Pending. Original error: {OriginalError}", job.Id, e.Message);
            }
        }

        return job;
    }

    public async Task<JobStatus> GetJobStatus(Guid id)
    {
        var job = await jobRepository.GetJobByIdAsync(id);
        return job?.Status ?? throw new NotFoundException($"Job with id {id} not found");
    }

    public async Task ProcessJob(Guid id)
    {
        var job = await jobRepository.GetJobByIdAsync(id) ?? throw new NotFoundException($"Job with id {id} not found");
        job.Status = JobStatus.Processing;
        job.UpdatedAt = DateTime.UtcNow;
        job.Attempts += 1;
        try
        {
            await jobRepository.UpdateAsync(job);
            await eventPublisher.PublishJobStatusUpdate(job.Id, job.Status);

            job.Status = JobStatus.Completed;
            job.UpdatedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job);
        }
        catch (Exception e)
        {
            job.Status = job.Attempts >= JobConstants.MaxAttempts ? JobStatus.DeadLetter : JobStatus.Pending;
            job.UpdatedAt = DateTime.UtcNow;
            job.RetryAt = DateTime.UtcNow + new TimeSpan(0, 5, Random.Shared.Next(0, 60));
            job.ErrorMessage = e.Message;
            try
            {
                await jobRepository.UpdateAsync(job);
            }
            catch (Exception dbEx)
            {
                // Could not persist the failure itself (e.g. SQL Server unreachable). The job is
                // now stuck at Status = Processing with no recorded error and no RetryAt saved —
                // GetPendingJobsAsync only fetches Status == Pending, so it will never be picked
                // up again on its own. Logged loudly on purpose: this means a job is lost, not
                // just "an error happened." Full recovery for this case is out of scope for now
                // (see Task 18 — outbox pattern / dead-letter reprocessing).
                logger.LogCritical(dbEx, "Failed to persist failure state for job {JobId} — job is now stuck at Processing. Original error: {OriginalError}", job.Id, e.Message);
            }
        }

        try
        {
            await eventPublisher.PublishJobStatusUpdate(job.Id, job.Status);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Redis not working");
        }
    }

    public async Task<Job> GetJobById(Guid id)
    {
        return await jobRepository.GetJobByIdAsync(id);
    }
}
