using JobQueue.Core.Enums;

namespace JobQueue.Core.Models;

public class Job
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Attempts { get; set; }
    public string? ErrorMessage { get; set; }
}