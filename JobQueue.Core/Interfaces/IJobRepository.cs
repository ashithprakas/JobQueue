using JobQueue.Core.Enums;
using JobQueue.Core.Models;

namespace JobQueue.Core.Interfaces;

public interface IJobRepository
{ 
    Task AddAsync(Job job);
    Task<Job?> GetJobByIdAsync(Guid id);
    Task UpdateAsync(Job job);
    Task<List<Job>>GetJobsToRetryAsync();
}