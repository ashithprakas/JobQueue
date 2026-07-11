using JobQueue.Core.Enums;
using JobQueue.Core.Models;
using JobQueue.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.Infrastructure.Repositories;

using Core.Interfaces;

public class JobRepository(AppDbContext appDbContext) : IJobRepository 
{
    public async Task AddAsync(Job job)
    {
        appDbContext.Jobs.Add(job);
        await appDbContext.SaveChangesAsync();
    }

    public async Task<Job?> GetJobByIdAsync(Guid id)
    { 
        return await appDbContext.Jobs.Where(job => job.Id == id ).FirstOrDefaultAsync();
    }

    public async Task<List<Job>> GetPendingJobsAsync()
    {
        return await appDbContext.Jobs.Where(job => job.Status == JobStatus.Pending ).Where(job=>job.RetryAt==null || job.RetryAt<DateTime.UtcNow).ToListAsync();
    }

    public async Task UpdateAsync(Job job)
    {
         await appDbContext.Jobs
             .Where(data => data.Id == job.Id)
             .ExecuteUpdateAsync(setter => setter
                 .SetProperty(u=>u.Status,job.Status)
                 .SetProperty(u=>u.Attempts,job.Attempts)
                 .SetProperty(u=>u.UpdatedAt,job.UpdatedAt)
                 .SetProperty(u=>u.ErrorMessage,job.ErrorMessage)
                 .SetProperty(u=>u.Payload,job.Payload)
                 .SetProperty(u=>u.RetryAt,job.RetryAt));
    }
    
}