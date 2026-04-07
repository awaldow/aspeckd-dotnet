using System.Text.Json;
using Aspeckd.Models;
using Aspeckd.Services;

namespace Aspeckd.Tests;

/// <summary>
/// Unit tests for <see cref="AgentSpecFileWriter"/> using a stub provider so that
/// the writer behaviour can be verified in isolation from ASP.NET Core discovery.
/// </summary>
public class AgentSpecFileWriterTests : IDisposable
{
    private readonly string _tempDir;

    public AgentSpecFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aspeckd-writer-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task WriteAsync_CreatesOutputDirectory()
    {
        Assert.False(Directory.Exists(_tempDir));

        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir);

        Assert.True(Directory.Exists(_tempDir));
    }

    [Fact]
    public async Task WriteAsync_WritesIndexFile()
    {
        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir);

        Assert.True(File.Exists(Path.Combine(_tempDir, "index.json")));
    }

    [Fact]
    public async Task WriteAsync_WritesSchemasFile()
    {
        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir);

        Assert.True(File.Exists(Path.Combine(_tempDir, "schemas.json")));
    }

    [Fact]
    public async Task WriteAsync_WritesDetailFileForEachEndpoint()
    {
        var provider = new StubAgentSpecProvider();
        await AgentSpecFileWriter.WriteAsync(provider, _tempDir);

        foreach (var ep in provider.GetIndex().Endpoints)
        {
            // Derive the file name from the last segment of the detailUrl.
            var id = AgentSpecFileWriter.ExtractId(ep.DetailUrl);
            Assert.True(
                File.Exists(Path.Combine(_tempDir, $"{id}.json")),
                $"Expected detail file for endpoint '{id}'.");
        }
    }

    [Fact]
    public async Task WriteAsync_IndexFileIsValidJson()
    {
        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir);

        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "index.json"));
        var doc = JsonDocument.Parse(content); // throws if not valid JSON
        Assert.Equal("Stub API", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WriteAsync_SchemasFileIsJsonArray()
    {
        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir);

        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "schemas.json"));
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task WriteAsync_DetailFileContainsExpectedId()
    {
        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir);

        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "get-stub.json"));
        var doc = JsonDocument.Parse(content);
        Assert.Equal("get-stub", doc.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task WriteAsync_UsesCustomSerializerOptions()
    {
        // Non-indented options — verify the file is not indented.
        var compact = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), _tempDir, compact);

        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "index.json"));
        Assert.DoesNotContain('\n', content);
    }

    [Fact]
    public async Task WriteAsync_DoesNotWriteDetailFileWhenProviderReturnsNull()
    {
        var provider = new StubAgentSpecProvider(returnNullDetail: true);
        await AgentSpecFileWriter.WriteAsync(provider, _tempDir);

        // The index lists the endpoint but no detail file should be written.
        Assert.False(File.Exists(Path.Combine(_tempDir, "get-stub.json")));
    }

    // -----------------------------------------------------------------------
    // Argument validation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_ThrowsWhenProviderIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => AgentSpecFileWriter.WriteAsync(null!, _tempDir));
    }

    [Fact]
    public async Task WriteAsync_ThrowsWhenOutputDirectoryIsNullOrEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => AgentSpecFileWriter.WriteAsync(new StubAgentSpecProvider(), string.Empty));
    }

    // -----------------------------------------------------------------------
    // Stub provider
    // -----------------------------------------------------------------------

    private sealed class StubAgentSpecProvider : IAgentSpecProvider
    {
        private readonly bool _returnNullDetail;

        public StubAgentSpecProvider(bool returnNullDetail = false)
        {
            _returnNullDetail = returnNullDetail;
        }

        public AgentSpecIndex GetIndex() => new()
        {
            Title = "Stub API",
            Description = "A stub API for testing.",
            SchemasUrl = "/agents/schemas",
            Endpoints =
            [
                new AgentIndexEntry
                {
                    Name = "GetStub",
                    Description = "A stub endpoint.",
                    DetailUrl = "/agents/get-stub"
                }
            ]
        };

        public AgentEndpointDetail? GetEndpointDetail(string id)
        {
            if (_returnNullDetail) return null;

            return id == "get-stub"
                ? new AgentEndpointDetail
                {
                    Id = "get-stub",
                    Name = "GetStub",
                    HttpMethod = "GET",
                    Route = "/stub",
                    Description = "A stub endpoint."
                }
                : null;
        }

        public IReadOnlyList<AgentSchemaInfo> GetSchemas() =>
        [
            new AgentSchemaInfo { Name = "StubSchema" }
        ];
    }
}
