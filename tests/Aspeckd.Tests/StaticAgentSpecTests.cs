using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspeckd.Models;
using Aspeckd.Services;
using Aspeckd.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Aspeckd.Tests;

/// <summary>
/// Verifies that <see cref="AgentSpecFileWriter"/> writes the spec tree to disk and that
/// <see cref="StaticSpecTestWebAppFactory"/> (which uses <c>AddStaticAgentSpec</c>) serves
/// the same data as the runtime provider.
/// </summary>
public class StaticAgentSpecTests : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly IAgentSpecProvider _runtimeProvider;
    private StaticSpecTestWebAppFactory? _staticFactory;
    private HttpClient? _staticClient;
    private string? _tempDir;

    public StaticAgentSpecTests(TestWebAppFactory factory)
    {
        _runtimeProvider = factory.Services.GetRequiredService<IAgentSpecProvider>();
    }

    public async Task InitializeAsync()
    {
        // Generate the static spec into a temporary directory, then spin up the
        // static-serving factory backed by those files.
        _tempDir = Path.Combine(Path.GetTempPath(), $"aspeckd-static-{Guid.NewGuid():N}");
        await AgentSpecFileWriter.WriteAsync(_runtimeProvider, _tempDir);

        _staticFactory = new StaticSpecTestWebAppFactory(_tempDir);
        _staticClient = _staticFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_staticFactory is not null)
            await _staticFactory.DisposeAsync();

        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // AgentSpecFileWriter – file output validation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_CreatesIndexFile()
    {
        var path = Path.Combine(_tempDir!, "index.json");
        Assert.True(File.Exists(path), "index.json should be written to the output directory.");

        var content = await File.ReadAllTextAsync(path);
        var doc = JsonDocument.Parse(content);
        Assert.Equal("Test API", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WriteAsync_CreatesSchemasFile()
    {
        var path = Path.Combine(_tempDir!, "schemas.json");
        Assert.True(File.Exists(path), "schemas.json should be written to the output directory.");

        var content = await File.ReadAllTextAsync(path);
        var schemas = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, schemas.RootElement.ValueKind);
    }

    [Fact]
    public void WriteAsync_CreatesPerEndpointDetailFiles()
    {
        var index = _runtimeProvider.GetIndex();
        foreach (var endpoint in index.Endpoints)
        {
            var path = Path.Combine(_tempDir!, $"{endpoint.Id}.json");
            Assert.True(File.Exists(path), $"{endpoint.Id}.json should be written for each endpoint.");
        }
    }

    [Fact]
    public async Task WriteAsync_IndexFileContainsCorrectEndpointCount()
    {
        var runtimeIndex = _runtimeProvider.GetIndex();
        var path = Path.Combine(_tempDir!, "index.json");
        var content = await File.ReadAllTextAsync(path);
        var fileIndex = JsonSerializer.Deserialize<AgentSpecIndex>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(fileIndex);
        Assert.Equal(runtimeIndex.Endpoints.Count, fileIndex.Endpoints.Count);
    }

    // -----------------------------------------------------------------------
    // Static provider – GET /agents (index)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Static_GetIndex_ReturnsOk()
    {
        var response = await _staticClient!.GetAsync("/agents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Static_GetIndex_MatchesRuntimeIndex()
    {
        var runtimeIndex = _runtimeProvider.GetIndex();
        var staticIndex = await _staticClient!.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(staticIndex);
        Assert.Equal(runtimeIndex.Title, staticIndex.Title);
        Assert.Equal(runtimeIndex.Description, staticIndex.Description);
        Assert.Equal(runtimeIndex.SchemasUrl, staticIndex.SchemasUrl);
        Assert.Equal(runtimeIndex.Endpoints.Count, staticIndex.Endpoints.Count);
    }

    [Fact]
    public async Task Static_GetIndex_ContainsHelloEndpoint()
    {
        var index = await _staticClient!.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var hello = index.Endpoints.FirstOrDefault(e => e.Route == "/api/hello");
        Assert.NotNull(hello);
        Assert.Equal("Hello", hello.Name);
        Assert.Equal("Says hello", hello.Description);
        Assert.Equal("GET", hello.HttpMethod);
    }

    // -----------------------------------------------------------------------
    // Static provider – GET /agents/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Static_GetEndpointDetail_ReturnsOkForKnownEndpoint()
    {
        var index = await _staticClient!.GetFromJsonAsync<AgentSpecIndex>("/agents");
        Assert.NotNull(index);

        var hello = index.Endpoints.First(e => e.Route == "/api/hello");
        var response = await _staticClient!.GetAsync(hello.DetailUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Static_GetEndpointDetail_MatchesRuntimeDetail()
    {
        var runtimeDetail = _runtimeProvider.GetEndpointDetail("get-api-hello");
        Assert.NotNull(runtimeDetail);

        var staticDetail = await _staticClient!.GetFromJsonAsync<AgentEndpointDetail>("/agents/get-api-hello");
        Assert.NotNull(staticDetail);

        Assert.Equal(runtimeDetail.Id, staticDetail.Id);
        Assert.Equal(runtimeDetail.Name, staticDetail.Name);
        Assert.Equal(runtimeDetail.Description, staticDetail.Description);
        Assert.Equal(runtimeDetail.HttpMethod, staticDetail.HttpMethod);
        Assert.Equal(runtimeDetail.Route, staticDetail.Route);
    }

    [Fact]
    public async Task Static_GetEndpointDetail_ReturnsNotFoundForUnknownId()
    {
        var response = await _staticClient!.GetAsync("/agents/does-not-exist-xyz");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Static_GetEndpointDetail_RejectsPathTraversalId()
    {
        // An id with directory-traversal characters should be rejected (404), not leak files.
        var response = await _staticClient!.GetAsync("/agents/..%2F..%2Fetc%2Fpasswd");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Static_GetEndpointDetail_RejectsIdWithUppercase()
    {
        // IDs are always lowercase; an uppercase id should not be found.
        var response = await _staticClient!.GetAsync("/agents/GET-API-HELLO");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Static provider – GET /agents/schemas
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Static_GetSchemas_ReturnsOk()
    {
        var response = await _staticClient!.GetAsync("/agents/schemas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Static_GetSchemas_MatchesRuntimeSchemas()
    {
        var runtimeSchemas = _runtimeProvider.GetSchemas();
        var staticSchemas = await _staticClient!.GetFromJsonAsync<List<AgentSchemaInfo>>("/agents/schemas");

        Assert.NotNull(staticSchemas);
        Assert.Equal(runtimeSchemas.Count, staticSchemas.Count);
    }
}
