using JobQueue.Core.Enums;
using JobQueue.Core.Models;

namespace JobQueue.Core.interfaces;

public interface IJobRepository
{ 
    Task AddAsync(Job job);
    Task<Job?> GetJobByIdAsync(Guid id);
    Task<List<Job>> GetPendingJobsAsync();
    Task UpdateAsync(Job job);
}