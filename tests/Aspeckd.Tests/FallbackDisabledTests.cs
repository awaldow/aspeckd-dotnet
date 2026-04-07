using System.Net.Http.Json;
using Aspeckd.Models;
using Aspeckd.Tests.Helpers;

namespace Aspeckd.Tests;

/// <summary>
/// Verifies that OpenAPI metadata does NOT bleed into the agent spec when
/// <see cref="Configuration.AspeckdOptions.UseOpenApiMetadataFallback"/> is <c>false</c> (the default).
/// </summary>
public class FallbackDisabledTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public FallbackDisabledTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WhenFallbackDisabled_WithSummaryDoesNotAppearAsDescription()
    {
        // The /api/items endpoint in TestWebAppFactory has no AgentDescriptionAttribute.
        // Its description should be null even though it has Accepts<SampleRequest> metadata.
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // No AgentName → auto-generated name is "POST /api/items".
        var items = index.Endpoints.FirstOrDefault(e => e.Name == "POST /api/items");
        Assert.NotNull(items);
        Assert.Null(items.Description);
    }

    [Fact]
    public async Task WhenFallbackDisabled_DefaultNameIsMethodPlusRoute()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // /api/items uses .WithName("CreateItem") in the test app but has no AgentNameAttribute.
        // With fallback off, the name should fall back to "POST /api/items".
        Assert.Contains(index.Endpoints, e => e.Name == "POST /api/items");
    }
}
