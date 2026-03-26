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
        var hello = index.Endpoints.FirstOrDefault(e => e.Route == "/api/hello");
        Assert.NotNull(hello);
        Assert.Equal("Hello", hello.Name);
        Assert.Equal("Says hello", hello.Description);
        Assert.Equal("GET", hello.HttpMethod);
    }

    [Fact]
    public async Task GetIndex_ExcludesHiddenEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.DoesNotContain(index.Endpoints, e => e.Route == "/api/hidden");
    }

    [Fact]
    public async Task GetIndex_IncludesPostItemsEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Route == "/api/items" && e.HttpMethod == "POST");
    }

    [Fact]
    public async Task GetIndex_EndpointDetailUrlHasCorrectShape()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var hello = index.Endpoints.First(e => e.Route == "/api/hello");
        Assert.StartsWith("/agents/", hello.DetailUrl);
        Assert.NotEmpty(hello.Id);
        Assert.Equal($"/agents/{hello.Id}", hello.DetailUrl);
    }

    // -----------------------------------------------------------------------
    // GET /agents/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetEndpointDetail_ReturnsOkForKnownEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");
        Assert.NotNull(index);

        var hello = index.Endpoints.First(e => e.Route == "/api/hello");
        var response = await _client.GetAsync(hello.DetailUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEndpointDetail_ReturnsCorrectData()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");
        Assert.NotNull(index);

        var hello = index.Endpoints.First(e => e.Route == "/api/hello");
        var detail = await _client.GetFromJsonAsync<AgentEndpointDetail>(hello.DetailUrl);

        Assert.NotNull(detail);
        Assert.Equal(hello.Id, detail.Id);
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
