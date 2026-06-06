using JobQueue.Core.Enums;

namespace JobQueue.Core.interfaces;

public interface IJobNotificationService
{
    Task NotifyJobStatusChange(Guid jobId, JobStatus jobStatus);
}