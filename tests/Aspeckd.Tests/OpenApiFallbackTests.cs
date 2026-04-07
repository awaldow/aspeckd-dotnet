using System.Net.Http.Json;
using Aspeckd.Models;
using Aspeckd.Tests.Helpers;

namespace Aspeckd.Tests;

/// <summary>
/// Verifies that standard OpenAPI metadata (<c>WithSummary</c>, <c>WithDescription</c>,
/// <c>WithName</c>) is surfaced in the agent spec index and detail endpoints when
/// <see cref="Configuration.AspeckdOptions.UseOpenApiMetadataFallback"/> is <c>true</c>.
/// </summary>
public class OpenApiFallbackTests : IClassFixture<FallbackTestWebAppFactory>
{
    private readonly HttpClient _client;

    public OpenApiFallbackTests(FallbackTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -----------------------------------------------------------------------
    // Name resolution via IEndpointNameMetadata (WithName)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_UsesWithNameAsEndpointName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // WithName("GetProducts") is the name source when UseOpenApiMetadataFallback=true.
        var products = index.Endpoints.FirstOrDefault(e => e.Name == "GetProducts");
        Assert.NotNull(products);
    }

    [Fact]
    public async Task Fallback_UsesWithNameForCategoryEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var categories = index.Endpoints.FirstOrDefault(e => e.Name == "GetCategories");
        Assert.NotNull(categories);
    }

    // -----------------------------------------------------------------------
    // Description resolution: WithSummary > WithDescription
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_PrefersWithSummaryOverWithDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var products = index.Endpoints.FirstOrDefault(e => e.Name == "GetProducts");
        Assert.NotNull(products);
        // WithSummary wins over WithDescription.
        Assert.Equal("Lists all products", products.Description);
    }

    [Fact]
    public async Task Fallback_UsesWithDescriptionWhenNoSummary()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var tags = index.Endpoints.FirstOrDefault(e => e.Name == "GetTags");
        Assert.NotNull(tags);
        Assert.Equal("Returns all tags in the system.", tags.Description);
    }

    [Fact]
    public async Task Fallback_UsesWithSummaryWhenOnlySummaryPresent()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var categories = index.Endpoints.FirstOrDefault(e => e.Name == "GetCategories");
        Assert.NotNull(categories);
        Assert.Equal("Lists all categories", categories.Description);
    }

    // -----------------------------------------------------------------------
    // Agent attributes take precedence over OpenAPI metadata
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_AgentDescriptionAttributeTakesPrecedenceOverWithSummary()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var orders = index.Endpoints.FirstOrDefault(e => e.Name == "OrdersOverride");
        Assert.NotNull(orders);
        Assert.Equal("Agent-specific description", orders.Description);
    }

    [Fact]
    public async Task Fallback_AgentNameAttributeTakesPrecedenceOverWithName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // AgentNameAttribute("OrdersOverride") wins over WithName("GetOrders").
        var orders = index.Endpoints.FirstOrDefault(e => e.Name == "OrdersOverride");
        Assert.NotNull(orders);
    }

    // -----------------------------------------------------------------------
    // Endpoint with no metadata at all
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_EndpointWithNoMetadataHasNullDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var bare = index.Endpoints.FirstOrDefault(e => e.Name == "GET /api/bare");
        Assert.NotNull(bare);
        Assert.Null(bare.Description);
    }

    [Fact]
    public async Task Fallback_EndpointWithNoMetadataHasDefaultName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // Default name is "METHOD /route" when no attribute or OpenAPI name is set.
        Assert.Contains(index.Endpoints, e => e.Name == "GET /api/bare");
    }

    // -----------------------------------------------------------------------
    // Detail endpoint reflects same resolution
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_DetailEndpointReflectsOpenApiName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");
        Assert.NotNull(index);

        var products = index.Endpoints.First(e => e.Name == "GetProducts");
        var detail = await _client.GetFromJsonAsync<AgentEndpointDetail>(products.DetailUrl);

        Assert.NotNull(detail);
        Assert.Equal("GetProducts", detail.Name);
        Assert.Equal("Lists all products", detail.Description);
    }
}
