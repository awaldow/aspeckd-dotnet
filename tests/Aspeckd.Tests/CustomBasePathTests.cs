using System.Net;
using System.Net.Http.Json;
using Aspeckd.Models;
using Aspeckd.Tests.Helpers;

namespace Aspeckd.Tests;

/// <summary>
/// Verifies that the agent spec endpoints are correctly served under a custom base path.
/// </summary>
public class CustomBasePathTests
{
    [Fact]
    public async Task GetIndex_IsServedAtCustomBasePath()
    {
        await using var factory = new TestWebAppFactory { AgentsBasePath = "/ai" };
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ai");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIndex_SchemasUrlReflectsCustomBasePath()
    {
        await using var factory = new TestWebAppFactory { AgentsBasePath = "/ai" };
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/ai");
        Assert.NotNull(index);
        Assert.Equal("/ai/schemas", index.SchemasUrl);
    }

    [Fact]
    public async Task GetIndex_DetailUrlsReflectCustomBasePath()
    {
        await using var factory = new TestWebAppFactory { AgentsBasePath = "/ai" };
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/ai");
        Assert.NotNull(index);
        Assert.All(index.Endpoints, e => Assert.StartsWith("/ai/", e.DetailUrl));
    }

    [Fact]
    public async Task GetSchemas_IsServedAtCustomBasePath()
    {
        await using var factory = new TestWebAppFactory { AgentsBasePath = "/ai" };
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ai/schemas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DefaultPath_DoesNotRespondWhenCustomPathSet()
    {
        await using var factory = new TestWebAppFactory { AgentsBasePath = "/ai" };
        var client = factory.CreateClient();

        var response = await client.GetAsync("/.well-known/agents");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
