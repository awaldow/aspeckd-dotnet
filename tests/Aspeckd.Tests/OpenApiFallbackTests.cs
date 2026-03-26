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
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var products = index.Endpoints.FirstOrDefault(e => e.Route == "/api/products");
        Assert.NotNull(products);
        Assert.Equal("GetProducts", products.Name);
    }

    [Fact]
    public async Task Fallback_UsesWithNameForCategoryEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var categories = index.Endpoints.FirstOrDefault(e => e.Route == "/api/categories");
        Assert.NotNull(categories);
        Assert.Equal("GetCategories", categories.Name);
    }

    // -----------------------------------------------------------------------
    // Description resolution: WithSummary > WithDescription
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_PrefersWithSummaryOverWithDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var products = index.Endpoints.FirstOrDefault(e => e.Route == "/api/products");
        Assert.NotNull(products);
        // WithSummary wins over WithDescription.
        Assert.Equal("Lists all products", products.Description);
    }

    [Fact]
    public async Task Fallback_UsesWithDescriptionWhenNoSummary()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var tags = index.Endpoints.FirstOrDefault(e => e.Route == "/api/tags");
        Assert.NotNull(tags);
        Assert.Equal("Returns all tags in the system.", tags.Description);
    }

    [Fact]
    public async Task Fallback_UsesWithSummaryWhenOnlySummaryPresent()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var categories = index.Endpoints.FirstOrDefault(e => e.Route == "/api/categories");
        Assert.NotNull(categories);
        Assert.Equal("Lists all categories", categories.Description);
    }

    // -----------------------------------------------------------------------
    // Agent attributes take precedence over OpenAPI metadata
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_AgentDescriptionAttributeTakesPrecedenceOverWithSummary()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var orders = index.Endpoints.FirstOrDefault(e => e.Route == "/api/orders");
        Assert.NotNull(orders);
        Assert.Equal("Agent-specific description", orders.Description);
    }

    [Fact]
    public async Task Fallback_AgentNameAttributeTakesPrecedenceOverWithName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var orders = index.Endpoints.FirstOrDefault(e => e.Route == "/api/orders");
        Assert.NotNull(orders);
        Assert.Equal("OrdersOverride", orders.Name);
    }

    // -----------------------------------------------------------------------
    // Endpoint with no metadata at all
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_EndpointWithNoMetadataHasNullDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var bare = index.Endpoints.FirstOrDefault(e => e.Route == "/api/bare");
        Assert.NotNull(bare);
        Assert.Null(bare.Description);
    }

    [Fact]
    public async Task Fallback_EndpointWithNoMetadataHasDefaultName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var bare = index.Endpoints.FirstOrDefault(e => e.Route == "/api/bare");
        Assert.NotNull(bare);
        // Default name is "METHOD /route" when no attribute or OpenAPI name is set.
        Assert.Equal("GET /api/bare", bare.Name);
    }

    // -----------------------------------------------------------------------
    // Detail endpoint reflects same resolution
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Fallback_DetailEndpointReflectsOpenApiName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");
        Assert.NotNull(index);

        var products = index.Endpoints.First(e => e.Route == "/api/products");
        var detail = await _client.GetFromJsonAsync<AgentEndpointDetail>(products.DetailUrl);

        Assert.NotNull(detail);
        Assert.Equal("GetProducts", detail.Name);
        Assert.Equal("Lists all products", detail.Description);
    }
}
