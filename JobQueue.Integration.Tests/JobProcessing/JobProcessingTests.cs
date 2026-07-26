using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobQueue.API.DTOs;
using JobQueue.Core.Enums;
using JobQueue.Integration.Tests.Constants;
using JobQueue.Integration.Tests.Fixtures;
[assembly: AssemblyFixture(typeof(JobQueue.Integration.Tests.Fixtures.ContainerFixtures))]
namespace JobQueue.Integration.Tests.JobProcessing;

public class JobProcessingTests(ContainerFixtures containers) : IAsyncLifetime
{
    private WorkerHostFixture _workerFactory = null!;
    private WebApplicationFixture _factory = null!;
    private HttpClient _client = null!;
    private static readonly JsonSerializerOptions StatusJsonOptions = new() { Converters = { new JsonStringEnumConverter() } };
    public async ValueTask InitializeAsync()
    {
        _workerFactory = new WorkerHostFixture(containers);
        await _workerFactory.InitializeAsync();
        _factory = new WebApplicationFixture(containers);
        _client = _factory.CreateClient();
    }
    public async ValueTask DisposeAsync()
    {
        await _workerFactory.DisposeAsync();
        await _factory.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task PostJob_WhenProcessedSuccessfully_ReachesCompletedStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var mockRequest = new CreateJobRequest { Id = jobId, Payload = "Test Payload" };

        // Act
        var httpResponse = await _client.PostAsJsonAsync("/jobs", mockRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        var initialStatus = await GetStatusAsync(jobId, TestContext.Current.CancellationToken);
        Assert.Equal(JobStatus.Pending, initialStatus);

        // Assert
        var finalStatus = await PollForStatusAsync(jobId, JobStatus.Completed, TestContext.Current.CancellationToken);
        Assert.Equal(JobStatus.Completed, finalStatus);
    }

    private async Task<JobStatus> GetStatusAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"jobs/{jobId}/status", cancellationToken);
        return await response.Content.ReadFromJsonAsync<JobStatus>(StatusJsonOptions, cancellationToken);
    }

    private async Task<JobStatus> PollForStatusAsync(Guid jobId, JobStatus expectedStatus, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(TimeConstants.MaxTestTimeoutMS);
        var pollInterval = TimeSpan.FromMilliseconds(TimeConstants.HttpRequestPollIntervalMS);
        var stopwatch = Stopwatch.StartNew();
        var status = JobStatus.Pending;

        while (stopwatch.Elapsed < timeout)
        {
            status = await GetStatusAsync(jobId, cancellationToken);

            if (status == expectedStatus)
            {
                return status;
            }

            if (status == JobStatus.Failed)
            {
                Assert.Fail($"Job {jobId} failed while waiting for status {expectedStatus}.");
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        Assert.Fail($"Timed out after {timeout.TotalSeconds}s waiting for status {expectedStatus}. Last observed status: {status}.");
        return status;
    }
}