using JobQueue.Core.Models;

namespace JobQueue.Core.Interfaces;

public interface IJobStreamService
{
    Task AddJobToQueueAsync(string id);
    Task EnsureConsumerGroupAsync();
    Task AcknowledgeAsync(string id);
    Task<List<StreamJobEntry>> ReadJobsAsync(string consumerName, int count);
}