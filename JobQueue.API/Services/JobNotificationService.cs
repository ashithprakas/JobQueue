using JobQueue.API.Hubs;
using JobQueue.Core.Enums;
using JobQueue.Core.interfaces;
using Microsoft.AspNetCore.SignalR;

namespace JobQueue.API.Services;

public class JobNotificationService(IHubContext<JobStatusHub> hubContext): IJobNotificationService
{
    public async Task NotifyJobStatusChange(Guid jobId, JobStatus jobStatus)
    {
        Console.WriteLine($"NotifyJobStatusChange: {jobId}");
        await hubContext.Clients.All.SendAsync("JobStatusChange", jobId, jobStatus);
    }
}