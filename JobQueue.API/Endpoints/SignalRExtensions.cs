using JobQueue.API.Hubs;

namespace JobQueue.API.Endpoints;

public static class SignalRExtensions
{
    public static void MapSignalREndpoints(this WebApplication app)
    {
        app.MapHub<JobStatusHub>("hubs/job-status");
    }
}