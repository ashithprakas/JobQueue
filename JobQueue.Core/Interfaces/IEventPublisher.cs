using JobQueue.Core.Enums;

namespace JobQueue.Core.Interfaces;

public interface IEventPublisher
{
    Task PublishJobStatusUpdate(Guid jobId , JobStatus status);
}