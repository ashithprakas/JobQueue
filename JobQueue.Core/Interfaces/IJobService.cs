using JobQueue.Core.Enums;
using JobQueue.Core.Models;

namespace JobQueue.Core.Interfaces;

public interface IJobService
{
    Task<Job> CreateJob(Guid id,string payload);
    Task<JobStatus> GetJobStatus(Guid id);
    Task ProcessJob(Guid id);
    Task <Job> GetJobById(Guid id);
}