using System.Text.Json;
using Aspeckd.Models;

namespace Aspeckd.Services;

/// <summary>
/// An <see cref="IAgentSpecProvider"/> implementation that reads from a directory of
/// pre-generated JSON files produced by <see cref="AgentSpecFileWriter"/>.
/// </summary>
/// <remarks>
/// Expected file layout (relative to <c>staticFilesDirectory</c>):
/// <list type="bullet">
///   <item><c>index.json</c>   — the spec index</item>
///   <item><c>schemas.json</c> — all named schemas</item>
///   <item><c>{id}.json</c>    — one file per endpoint</item>
/// </list>
/// This provider is registered by <c>AddStaticAgentSpec()</c> and is suitable for
/// production deployments where the spec is baked in at build / publish time.
/// </remarks>
internal sealed class StaticFileAgentSpecProvider : IAgentSpecProvider
{
    private readonly string _directory;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public StaticFileAgentSpecProvider(string staticFilesDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staticFilesDirectory);
        _directory = staticFilesDirectory;
    }

    /// <inheritdoc/>
    public AgentSpecIndex GetIndex()
        => ReadJson<AgentSpecIndex>("index.json") ?? new AgentSpecIndex();

    /// <inheritdoc/>
    public AgentEndpointDetail? GetEndpointDetail(string id)
        => ReadJson<AgentEndpointDetail>($"{id}.json");

    /// <inheritdoc/>
    public IReadOnlyList<AgentSchemaInfo> GetSchemas()
        => ReadJson<List<AgentSchemaInfo>>("schemas.json") ?? [];

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private T? ReadJson<T>(string fileName)
    {
        var path = Path.Combine(_directory, fileName);
        if (!File.Exists(path))
            return default;

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, ReadOptions);
    }
}
