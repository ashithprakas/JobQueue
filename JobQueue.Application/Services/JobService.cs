using JobQueue.Core.Constants;
using JobQueue.Core.Enums;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using JobQueue.Core.Exceptions;

namespace JobQueue.Application.Services;

public class JobService(IJobRepository jobRepository , IEventPublisher eventPublisher, IJobStreamService redisRepository) : IJobService
{
    public async Task<Job> CreateJob(Guid Id,string payload)
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
        await redisRepository.AddJobToQueueAsync(job.Id.ToString());
        return job;
    } 

    public async Task<JobStatus> GetJobStatus(Guid id)
    {
        var job = await jobRepository.GetJobByIdAsync(id);
        return job?.Status ?? throw new NotFoundException($"Job with id {id} not found");
    }

    public async Task ProcessJob(Guid id)
    {
        var job = await jobRepository.GetJobByIdAsync(id);
        if (job == null)
        {
            throw new NotFoundException($"Job with id {id} not found");
        }
        job.Status = JobStatus.Processing;
        job.UpdatedAt = DateTime.UtcNow;
        job.Attempts +=1;
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
            job.RetryAt = DateTime.UtcNow + new TimeSpan(0,5,Random.Shared.Next(0, 60)) ;
            job.ErrorMessage = e.Message;
            try
            {
                await jobRepository.UpdateAsync(job);
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"CRITICAL: failed to persist failure state for job {job.Id} — job is now stuck at Processing. Original error: {e.Message}. Persistence error: {dbEx.Message}");
            }
        }

        try
        {
            await eventPublisher.PublishJobStatusUpdate(job.Id, job.Status);
        }
        catch (Exception e)
        {
            Console.WriteLine("Redis not working : Error - " + e.Message);
        }
    }

    public async Task<Job> GetJobById(Guid id)
    {
        return await jobRepository.GetJobByIdAsync(id);
    }
}