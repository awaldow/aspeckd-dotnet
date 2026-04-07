using System.Text.Json;
using System.Text.RegularExpressions;
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
    // Endpoint IDs are produced by AgentSpecProvider.BuildId which only outputs
    // lowercase letters, digits, and hyphens (e.g. "get-api-weather-forecast").
    // Reject anything outside that character set before building a file path so
    // that caller-supplied IDs cannot traverse out of the spec directory.
    private static readonly Regex ValidIdPattern = new(@"^[a-z0-9\-]+$", RegexOptions.Compiled);

    private readonly string _directory;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public StaticFileAgentSpecProvider(string staticFilesDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staticFilesDirectory);
        // Resolve to an absolute, canonical path once so that the confinement
        // check in ReadJson is reliable regardless of the working directory.
        _directory = Path.GetFullPath(staticFilesDirectory);
    }

    /// <inheritdoc/>
    public AgentSpecIndex GetIndex()
        => ReadJson<AgentSpecIndex>("index.json") ?? new AgentSpecIndex();

    /// <inheritdoc/>
    public AgentEndpointDetail? GetEndpointDetail(string id)
    {
        // Validate the id before using it in a file path to prevent path traversal.
        if (string.IsNullOrEmpty(id) || !ValidIdPattern.IsMatch(id))
            return null;

        return ReadJson<AgentEndpointDetail>($"{id}.json");
    }

    /// <inheritdoc/>
    public IReadOnlyList<AgentSchemaInfo> GetSchemas()
        => ReadJson<List<AgentSchemaInfo>>("schemas.json") ?? [];

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private T? ReadJson<T>(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(_directory, fileName));

        // Defense-in-depth: ensure the resolved path stays within the
        // configured spec directory even for the fixed file names.
        if (!path.StartsWith(_directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !string.Equals(path, _directory, StringComparison.Ordinal))
        {
            return default;
        }

        if (!File.Exists(path))
            return default;

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, ReadOptions);
    }
}
