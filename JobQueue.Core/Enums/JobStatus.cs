namespace JobQueue.Core.Enums;

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    DeadLetter
}