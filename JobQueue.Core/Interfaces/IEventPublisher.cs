using JobQueue.Core.Enums;

namespace JobQueue.Core.interfaces;

public interface IEventPublisher
{
    Task PublishJobStatusUpdate(Guid jobId , JobStatus status);
}