using System.Text.Json;
using Aspeckd.Models;

namespace Aspeckd.Services;

/// <summary>
/// Utility that serialises the agent spec produced by an <see cref="IAgentSpecProvider"/>
/// into a directory of static JSON files that can be committed, shipped in a publish
/// artifact, or served directly from a CDN / static-file middleware.
/// </summary>
/// <remarks>
/// The output layout mirrors the runtime URL tree:
/// <list type="bullet">
///   <item><c>index.json</c>      — the spec index (served at <c>{basePath}</c>)</item>
///   <item><c>schemas.json</c>    — all named schemas (served at <c>{basePath}/schemas</c>)</item>
///   <item><c>{id}.json</c>       — one file per endpoint (served at <c>{basePath}/{id}</c>)</item>
/// </list>
/// </remarks>
public static class AgentSpecFileWriter
{
    internal static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Extracts the stable endpoint identifier from a detail URL by returning the last
    /// non-empty path segment (e.g. <c>/.well-known/agents/get-api-weather</c> → <c>get-api-weather</c>).
    /// </summary>
    public static string ExtractId(string detailUrl)
    {
        var trimmed = detailUrl.TrimEnd('/');
        var slash = trimmed.LastIndexOf('/');
        return slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
    }

    /// <summary>
    /// Writes the full agent spec tree produced by <paramref name="provider"/> to
    /// <paramref name="outputDirectory"/>.  The directory is created when it does not exist.
    /// </summary>
    /// <param name="provider">The spec provider to read from.</param>
    /// <param name="outputDirectory">Destination directory (absolute or relative to the working directory).</param>
    /// <param name="options">
    /// Optional JSON serialiser options.  When <see langword="null"/>, camelCase, indented
    /// output is used so the files are both human-readable and compatible with
    /// <see cref="StaticFileAgentSpecProvider"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task WriteAsync(
        IAgentSpecProvider provider,
        string outputDirectory,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        options ??= DefaultOptions;

        Directory.CreateDirectory(outputDirectory);

        // index.json
        var index = provider.GetIndex();
        await WriteJsonFileAsync(Path.Combine(outputDirectory, "index.json"), index, options, cancellationToken);

        // schemas.json
        var schemas = provider.GetSchemas();
        await WriteJsonFileAsync(Path.Combine(outputDirectory, "schemas.json"), schemas, options, cancellationToken);

        // {id}.json — one file per endpoint.
        // The endpoint ID is the last path segment of the detailUrl
        // (e.g. "/.well-known/agents/get-api-weather" → "get-api-weather").
        foreach (var endpoint in index.Endpoints)
        {
            var id = ExtractId(endpoint.DetailUrl);
            if (string.IsNullOrEmpty(id))
                continue;

            var detail = provider.GetEndpointDetail(id);
            if (detail is not null)
            {
                var filePath = Path.Combine(outputDirectory, $"{id}.json");
                await WriteJsonFileAsync(filePath, detail, options, cancellationToken);
            }
        }
    }

    private static async Task WriteJsonFileAsync<T>(
        string filePath,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
    }
}
