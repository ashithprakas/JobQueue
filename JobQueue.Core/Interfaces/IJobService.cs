using JobQueue.Core.Enums;
using JobQueue.Core.Models;

namespace JobQueue.Core.interfaces;

public interface IJobService
{
    Task<Job> CreateJob(string payload);
    Task<JobStatus> GetJobStatus(Guid id);
    Task ProcessJob(Guid id);
}