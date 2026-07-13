using JobQueue.Core.Constants;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using StackExchange.Redis;

namespace JobQueue.Infrastructure.RedisRepository;

public class JobStreamService(IConnectionMultiplexer redis) : IJobStreamService
{
    public Task AddJobToQueueAsync(string id)
    {
        var db = redis.GetDatabase();
        return db.StreamAddAsync(JobStreamConstants.JOBSTREAMKEY, "jobId", id);
    }

    public async Task EnsureConsumerGroupAsync()
    {
        try
        {
            var db = redis.GetDatabase();
            await  db.StreamCreateConsumerGroupAsync(JobStreamConstants.JOBSTREAMKEY, JobStreamConstants.JOBWORKERGROUP, "0", createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            Console.WriteLine("Consumer group already exists");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public Task AcknowledgeAsync(string id)
    {
        var db = redis.GetDatabase();
        return db.StreamAcknowledgeAsync(JobStreamConstants.JOBSTREAMKEY, JobStreamConstants.JOBWORKERGROUP, id);
    }
    public async Task<List<StreamJobEntry>> ReadJobsAsync(string consumerName, int count)
    {
        var db = redis.GetDatabase();
        var entries = await db.StreamReadGroupAsync(JobStreamConstants.JOBSTREAMKEY, JobStreamConstants.JOBWORKERGROUP, consumerName, ">", count);
        var result = (from entry in entries let jobId = Guid.Parse(entry["jobId"].ToString()) select new StreamJobEntry(entry.Id.ToString(), jobId)).ToList();

        return result;
    }

    
    
}
