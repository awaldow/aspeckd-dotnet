using System.Text.Json;
using Aspeckd.Configuration;
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

    /// <summary>
    /// Writes a versioned agent spec tree to <paramref name="outputDirectory"/>.
    /// When <paramref name="aspeckdOptions"/> has <see cref="AspeckdOptions.Versions"/>
    /// configured, this method writes:
    /// <list type="bullet">
    ///   <item><c>index.json</c> — the <see cref="AgentVersionIndex"/> root listing all versions</item>
    ///   <item><c>{version}/index.json</c>, <c>{version}/schemas.json</c>, <c>{version}/{id}.json</c>
    ///     — one subdirectory per version</item>
    /// </list>
    /// When no versions are configured, this falls back to the non-versioned
    /// <see cref="WriteAsync(IAgentSpecProvider, string, JsonSerializerOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="provider">The spec provider to read from.</param>
    /// <param name="outputDirectory">Destination directory.</param>
    /// <param name="aspeckdOptions">Aspeckd options, used to build the version index and URL prefix filtering.</param>
    /// <param name="options">Optional JSON serialiser options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task WriteAsync(
        IAgentSpecProvider provider,
        string outputDirectory,
        AspeckdOptions aspeckdOptions,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(aspeckdOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        options ??= DefaultOptions;

        if (aspeckdOptions.Versions.Count == 0)
        {
            // Fall back to the non-versioned overload.
            await WriteAsync(provider, outputDirectory, options, cancellationToken);
            return;
        }

        Directory.CreateDirectory(outputDirectory);

        // Normalise the base path for building indexUrl values.
        var basePath = aspeckdOptions.BasePath.TrimEnd('/');
        if (!basePath.StartsWith('/'))
            basePath = $"/{basePath}";

        // Root index.json — AgentVersionIndex listing all versions.
        var versionIndex = new AgentVersionIndex
        {
            ApiTitle = aspeckdOptions.Title ?? "API",
            Description = aspeckdOptions.Description,
            Versions = aspeckdOptions.Versions
                .Select(v => new AgentVersionInfo
                {
                    Version = v.Version,
                    Status = v.Status,
                    SunsetDate = v.SunsetDate,
                    IndexUrl = $"{basePath}/{v.Version}"
                })
                .ToList(),
            DefaultVersion = aspeckdOptions.DefaultVersion
        };
        await WriteJsonFileAsync(Path.Combine(outputDirectory, "index.json"), versionIndex, options, cancellationToken);

        // Per-version subdirectories.
        var fullIndex = provider.GetIndex();
        foreach (var versionConfig in aspeckdOptions.Versions)
        {
            var versionDir = Path.Combine(outputDirectory, versionConfig.Version);
            var versionedBasePath = $"{basePath}/{versionConfig.Version}";
            var filteredIndex = BuildFilteredIndex(provider, fullIndex, versionConfig, aspeckdOptions, versionedBasePath);
            await WriteAsync(provider, versionDir, filteredIndex, options, cancellationToken);
        }
    }

    // Overload used by the versioned WriteAsync to write a pre-built filtered index.
    private static async Task WriteAsync(
        IAgentSpecProvider provider,
        string outputDirectory,
        AgentSpecIndex index,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        await WriteJsonFileAsync(Path.Combine(outputDirectory, "index.json"), index, options, cancellationToken);

        var schemas = provider.GetSchemas();
        await WriteJsonFileAsync(Path.Combine(outputDirectory, "schemas.json"), schemas, options, cancellationToken);

        foreach (var endpoint in index.Endpoints)
        {
            // Extract the id from the versioned detailUrl (last segment).
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

    // Builds a filtered AgentSpecIndex for a given version, scoped to versionedBasePath.
    internal static AgentSpecIndex BuildFilteredIndex(
        IAgentSpecProvider provider,
        AgentSpecIndex fullIndex,
        AspeckdVersionOptions versionConfig,
        AspeckdOptions aspeckdOptions,
        string versionedBasePath)
    {
        var filteredEndpoints = FilterAndRekeyEndpoints(
            provider, fullIndex.Endpoints, versionConfig.UrlPrefix, versionedBasePath);

        var filteredGroups = FilterAndRekeyGroups(
            provider, fullIndex.Groups, versionConfig.UrlPrefix, versionedBasePath);

        return new AgentSpecIndex
        {
            Title = versionConfig.Title ?? aspeckdOptions.Title ?? "API",
            Description = versionConfig.Description ?? aspeckdOptions.Description,
            SchemasUrl = $"{versionedBasePath}/schemas",
            Endpoints = filteredEndpoints,
            Groups = filteredGroups,
            Auth = aspeckdOptions.Auth
        };
    }

    // Filters and re-keys endpoint entries to use the versioned base path.
    private static IReadOnlyList<AgentIndexEntry> FilterAndRekeyEndpoints(
        IAgentSpecProvider provider,
        IReadOnlyList<AgentIndexEntry> entries,
        string? urlPrefix,
        string versionedBasePath)
    {
        var result = new List<AgentIndexEntry>();
        foreach (var entry in entries)
        {
            var id = ExtractId(entry.DetailUrl);
            if (string.IsNullOrEmpty(id))
                continue;

            if (!string.IsNullOrEmpty(urlPrefix))
            {
                var detail = provider.GetEndpointDetail(id);
                if (detail is null || !RouteMatchesPrefix(detail.Route, urlPrefix))
                    continue;
            }

            result.Add(new AgentIndexEntry
            {
                Name = entry.Name,
                Description = entry.Description,
                DetailUrl = $"{versionedBasePath}/{id}"
            });
        }
        return result;
    }

    // Filters and re-keys tool groups to use the versioned base path.
    private static IReadOnlyList<AgentToolGroup> FilterAndRekeyGroups(
        IAgentSpecProvider provider,
        IReadOnlyList<AgentToolGroup> groups,
        string? urlPrefix,
        string versionedBasePath)
    {
        var result = new List<AgentToolGroup>();
        foreach (var group in groups)
        {
            var filteredEndpoints = FilterAndRekeyEndpoints(
                provider, group.Endpoints, urlPrefix, versionedBasePath);
            if (filteredEndpoints.Count == 0)
                continue;

            result.Add(new AgentToolGroup
            {
                Name = group.Name,
                Description = group.Description,
                RequiredClaims = group.RequiredClaims,
                Endpoints = filteredEndpoints,
                Auth = group.Auth
            });
        }
        return result;
    }

    // Returns true when the route (e.g. "/api/v1/weather") starts with the given URL prefix
    // (e.g. "api/v1" or "/api/v1").
    internal static bool RouteMatchesPrefix(string route, string prefix)
    {
        var normalizedRoute = route.TrimStart('/');
        var normalizedPrefix = prefix.Trim('/');
        return normalizedRoute.StartsWith(normalizedPrefix + "/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedRoute, normalizedPrefix, StringComparison.OrdinalIgnoreCase);
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
