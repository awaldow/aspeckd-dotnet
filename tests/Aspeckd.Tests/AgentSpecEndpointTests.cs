using System.Net;
using System.Net.Http.Json;
using Aspeckd.Models;
using Aspeckd.Tests.Helpers;

namespace Aspeckd.Tests;

public class AgentSpecEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AgentSpecEndpointTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -----------------------------------------------------------------------
    // GET /agents
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_ReturnsOk()
    {
        var response = await _client.GetAsync("/agents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIndex_ContainsTitleAndDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.Equal("Test API", index.Title);
        Assert.Equal("Integration test API", index.Description);
    }

    [Fact]
    public async Task GetIndex_SchemasUrlPointsToSchemasEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.Equal("/agents/schemas", index.SchemasUrl);
    }

    [Fact]
    public async Task GetIndex_IncludesHelloEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var hello = index.Endpoints.FirstOrDefault(e => e.Name == "Hello");
        Assert.NotNull(hello);
        Assert.Equal("Says hello", hello.Description);
        Assert.StartsWith("/agents/", hello.DetailUrl);
    }

    [Fact]
    public async Task GetIndex_ExcludesHiddenEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        // The hidden endpoint has no AgentNameAttribute so its auto-generated name is
        // "GET /api/hidden". It must not appear in the index at all.
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GET /api/hidden");
    }

    [Fact]
    public async Task GetIndex_IncludesPostItemsEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        // No AgentNameAttribute on this endpoint → auto-generated name is "POST /api/items".
        Assert.Contains(index.Endpoints, e => e.Name == "POST /api/items");
    }

    [Fact]
    public async Task GetIndex_EndpointDetailUrlHasCorrectShape()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var hello = index.Endpoints.First(e => e.Name == "Hello");
        Assert.StartsWith("/agents/", hello.DetailUrl);
        Assert.NotEmpty(hello.DetailUrl.TrimStart('/').Split('/').Last());
    }

    // -----------------------------------------------------------------------
    // GET /agents/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetEndpointDetail_ReturnsOkForKnownEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");
        Assert.NotNull(index);

        var hello = index.Endpoints.First(e => e.Name == "Hello");
        var response = await _client.GetAsync(hello.DetailUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEndpointDetail_ReturnsCorrectData()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");
        Assert.NotNull(index);

        var hello = index.Endpoints.First(e => e.Name == "Hello");
        var detail = await _client.GetFromJsonAsync<AgentEndpointDetail>(hello.DetailUrl);

        Assert.NotNull(detail);
        Assert.Equal("Hello", detail.Name);
        Assert.Equal("Says hello", detail.Description);
        Assert.Equal("GET", detail.HttpMethod);
        Assert.Equal("/api/hello", detail.Route);
    }

    [Fact]
    public async Task GetEndpointDetail_ReturnsNotFoundForUnknownId()
    {
        var response = await _client.GetAsync("/agents/does-not-exist-xyz");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // GET /agents/schemas
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSchemas_ReturnsOk()
    {
        var response = await _client.GetAsync("/agents/schemas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSchemas_ReturnsList()
    {
        var schemas = await _client.GetFromJsonAsync<List<AgentSchemaInfo>>("/agents/schemas");
        Assert.NotNull(schemas);
    }
}
