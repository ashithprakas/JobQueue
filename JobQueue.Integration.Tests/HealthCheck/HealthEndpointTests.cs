using System.Net;
using System.Net.Http.Json;
using JobQueue.API.DTOs;
using JobQueue.Integration.Tests.Fixtures;

namespace JobQueue.Integration.Tests.HealthCheck;

public class HealthEndpointTests : IDisposable
{
    private readonly WebApplicationFixture _factory;
    private readonly HttpClient _client;
    public HealthEndpointTests(ContainerFixtures container)
    {
        _factory = new WebApplicationFixture(container);
        _client = _factory.CreateClient();
    }
    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<GenericResponse>(cancellationToken: TestContext.Current.CancellationToken);
        var testResponse = new GenericResponse() { Status = "Healthy" };
        Assert.NotNull(content);
        Assert.Equal(testResponse.Status, content.Status);
    }
}