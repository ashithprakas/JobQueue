using JobQueue.Core.Enums;
using JobQueue.Core.interfaces;

namespace JobQueue.Worker.Service;

public class NoOpJobNotificationSerivce : IJobNotificationService
{
    public Task NotifyJobStatusChange(Guid jobId, JobStatus jobStatus) => Task.CompletedTask;
}