using JobQueue.Core.Constants;
using JobQueue.Core.Enums;
using JobQueue.Core.interfaces;
using JobQueue.Core.Models;
using JobQueue.Core.Exceptions;

namespace JobQueue.application.Services;

public class JobService(IJobRepository jobRepository , IJobNotificationService jobNotificationService) : IJobService
{
    public async Task<Job> CreateJob(string payload)
    {
        var job = new Job()
        {
            Payload = payload,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Attempts = 0
        };
        await jobRepository.AddAsync(job);
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
            await jobNotificationService.NotifyJobStatusChange(job.Id, job.Status);
    
            job.Status = JobStatus.Completed;
            job.UpdatedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job);
        }
        catch (Exception e)
        {
            job.Status = job.Attempts >= JobConstants.MaxAttempts ? JobStatus.DeadLetter : JobStatus.Pending;
            job.UpdatedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job);
        }
        await jobNotificationService.NotifyJobStatusChange(job.Id, job.Status);
    }
    
    public async Task<List<Job>> GetPendingJobs()
    {
        var job = await jobRepository.GetPendingJobsAsync();
        return job;
    }
}