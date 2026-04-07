using System.Text.RegularExpressions;
using Aspeckd.Attributes;
using Aspeckd.Configuration;
using Aspeckd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Aspeckd.Services;

/// <summary>
/// Default implementation of <see cref="IAgentSpecProvider"/> that uses
/// <see cref="IApiDescriptionGroupCollectionProvider"/> to discover API endpoints.
/// </summary>
/// <remarks>
/// This class lives in the <c>Aspeckd</c> implementation project (not <c>Aspeckd.Core</c>) so
/// that TFM-specific behaviour — such as differences in <c>Microsoft.OpenApi</c> between
/// .NET 8/9 (v1.6) and .NET 10+ (v2+) — can be handled here with <c>#if</c> guards without
/// touching the stable core abstractions.
/// </remarks>
internal sealed class AgentSpecProvider : IAgentSpecProvider
{
    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionProvider;
    private readonly AspeckdOptions _options;

    public AgentSpecProvider(
        IApiDescriptionGroupCollectionProvider apiDescriptionProvider,
        IOptions<AspeckdOptions> options)
    {
        _apiDescriptionProvider = apiDescriptionProvider;
        _options = options.Value;
    }

    public AgentSpecIndex GetIndex()
    {
        var basePath = NormalizeBasePath(_options.BasePath);
        var descriptions = GetVisibleDescriptions().ToList();

        var endpoints = descriptions
            .Select(d => BuildSummary(d, basePath))
            .OrderBy(e => e.HttpMethod)
            .ThenBy(e => e.Route)
            .ToList();

        var groups = BuildGroups(descriptions, endpoints);

        return new AgentSpecIndex
        {
            Title = _options.Title ?? "API",
            Description = _options.Description,
            SchemasUrl = $"{basePath}/schemas",
            Endpoints = endpoints,
            Groups = groups
        };
    }

    public AgentEndpointDetail? GetEndpointDetail(string id)
    {
        var description = GetVisibleDescriptions()
            .FirstOrDefault(d => BuildId(d) == id);

        return description is null ? null : BuildDetail(description);
    }

    public IReadOnlyList<AgentSchemaInfo> GetSchemas()
    {
        // Collect unique response/request types from all visible endpoints.
        var types = new HashSet<Type>();
        foreach (var description in GetVisibleDescriptions())
        {
            foreach (var responseType in description.SupportedResponseTypes)
            {
                if (responseType.Type is not null && responseType.Type != typeof(void))
                    types.Add(responseType.Type);
            }

            foreach (var param in description.ParameterDescriptions)
            {
                if (param.Type is not null)
                    types.Add(param.Type);
            }
        }

        return types
            .OrderBy(t => t.Name)
            .Select(t => new AgentSchemaInfo { Name = t.Name })
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private IEnumerable<ApiDescription> GetVisibleDescriptions()
    {
        return _apiDescriptionProvider.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Where(d => !IsExcluded(d));
    }

    private static bool IsExcluded(ApiDescription description)
    {
        if (description.ActionDescriptor?.EndpointMetadata is null)
            return false;

        return description.ActionDescriptor.EndpointMetadata
            .OfType<AgentExcludeAttribute>()
            .Any();
    }

    private static string BuildId(ApiDescription description)
    {
        var method = (description.HttpMethod ?? "get").ToLowerInvariant();
        var route = description.RelativePath ?? string.Empty;
        // Strip leading slash and replace non-alphanumeric characters with hyphens.
        var slug = Regex.Replace(route.TrimStart('/'), @"[^a-z0-9]+", "-", RegexOptions.IgnoreCase)
                        .Trim('-')
                        .ToLowerInvariant();
        return string.IsNullOrEmpty(slug) ? method : $"{method}-{slug}";
    }

    /// <summary>
    /// Resolves the display name for an endpoint using the following priority:
    /// <list type="number">
    ///   <item><see cref="AgentNameAttribute"/> — always checked first.</item>
    ///   <item>
    ///     <see cref="IEndpointNameMetadata"/> (set by <c>WithName()</c>) — only when
    ///     <see cref="AspeckdOptions.UseOpenApiMetadataFallback"/> is <see langword="true"/>.
    ///   </item>
    ///   <item>Auto-generated from the HTTP method and route template.</item>
    /// </list>
    /// </summary>
    private string BuildName(ApiDescription description)
    {
        var metadata = description.ActionDescriptor?.EndpointMetadata;

        // 1. Explicit agent attribute always wins.
        var nameAttr = metadata?.OfType<AgentNameAttribute>().FirstOrDefault();
        if (nameAttr is not null)
            return nameAttr.Name;

        // 2. Optionally fall back to IEndpointNameMetadata (set by WithName()).
        if (_options.UseOpenApiMetadataFallback)
        {
            var endpointName = metadata?.OfType<IEndpointNameMetadata>()
                                        .FirstOrDefault()?.EndpointName;
            if (!string.IsNullOrWhiteSpace(endpointName))
                return endpointName;
        }

        // 3. Default: "METHOD /route".
        var method = (description.HttpMethod ?? "GET").ToUpperInvariant();
        var route = description.RelativePath ?? string.Empty;
        return string.IsNullOrEmpty(route) ? method : $"{method} /{route.TrimStart('/')}";
    }

    /// <summary>
    /// Resolves the description for an endpoint using the following priority:
    /// <list type="number">
    ///   <item><see cref="AgentDescriptionAttribute"/> — always checked first.</item>
    ///   <item>
    ///     <see cref="EndpointSummaryAttribute"/> (set by <c>WithSummary()</c>) — only when
    ///     <see cref="AspeckdOptions.UseOpenApiMetadataFallback"/> is <see langword="true"/>.
    ///   </item>
    ///   <item>
    ///     <see cref="EndpointDescriptionAttribute"/> (set by <c>WithDescription()</c>) — only
    ///     when <see cref="AspeckdOptions.UseOpenApiMetadataFallback"/> is
    ///     <see langword="true"/> and no summary is available.
    ///   </item>
    ///   <item><see langword="null"/> when no description source is found.</item>
    /// </list>
    /// </summary>
    private string? BuildDescription(ApiDescription description)
    {
        var metadata = description.ActionDescriptor?.EndpointMetadata;

        // 1. Explicit agent attribute always wins.
        var descAttr = metadata?.OfType<AgentDescriptionAttribute>().FirstOrDefault();
        if (descAttr is not null)
            return descAttr.Description;

        if (_options.UseOpenApiMetadataFallback)
        {
            // 2. WithSummary() — concise one-liner, ideal for the agent index.
            var summary = metadata?.OfType<EndpointSummaryAttribute>()
                                   .FirstOrDefault()?.Summary;
            if (!string.IsNullOrWhiteSpace(summary))
                return summary;

            // 3. WithDescription() — longer prose, used when no summary exists.
            var desc = metadata?.OfType<EndpointDescriptionAttribute>()
                                 .FirstOrDefault()?.Description;
            if (!string.IsNullOrWhiteSpace(desc))
                return desc;
        }

        return null;
    }

    private AgentEndpointSummary BuildSummary(ApiDescription description, string basePath)
    {
        var id = BuildId(description);
        var groupAttr = description.ActionDescriptor?.EndpointMetadata
            ?.OfType<AgentToolGroupAttribute>().FirstOrDefault();

        return new AgentEndpointSummary
        {
            Id = id,
            Name = BuildName(description),
            HttpMethod = (description.HttpMethod ?? "GET").ToUpperInvariant(),
            Route = $"/{(description.RelativePath ?? string.Empty).TrimStart('/')}",
            Description = BuildDescription(description),
            DetailUrl = $"{basePath}/{id}",
            Group = groupAttr?.Name
        };
    }

    /// <summary>
    /// Builds the list of <see cref="AgentToolGroup"/> objects from the visible endpoint
    /// descriptions and their already-built summaries. Groups preserve the attribute's
    /// <see cref="AgentToolGroupAttribute.Description"/> and
    /// <see cref="AgentToolGroupAttribute.RequiredClaims"/> from the first endpoint seen
    /// for that group name (an implicit "representative" endpoint).
    /// </summary>
    private static IReadOnlyList<AgentToolGroup> BuildGroups(
        IReadOnlyList<ApiDescription> descriptions,
        IReadOnlyList<AgentEndpointSummary> summaries)
    {
        // Build a lookup from endpoint id → group attribute for efficient access.
        var groupAttrsByEndpointId = new Dictionary<string, AgentToolGroupAttribute>(StringComparer.Ordinal);
        foreach (var d in descriptions)
        {
            var attr = d.ActionDescriptor?.EndpointMetadata
                ?.OfType<AgentToolGroupAttribute>().FirstOrDefault();
            if (attr is not null)
            {
                var id = BuildId(d);
                groupAttrsByEndpointId[id] = attr;
            }
        }

        // Group summaries by group name, preserving the attribute metadata.
        var toolGroupsByName = new Dictionary<string, (AgentToolGroupAttribute Attr, List<AgentEndpointSummary> Endpoints)>(
            StringComparer.Ordinal);

        foreach (var summary in summaries)
        {
            if (summary.Group is null)
                continue;

            if (!groupAttrsByEndpointId.TryGetValue(summary.Id, out var attr))
                continue;

            if (!toolGroupsByName.TryGetValue(summary.Group, out var entry))
            {
                entry = (attr, []);
                toolGroupsByName[summary.Group] = entry;
            }

            entry.Endpoints.Add(summary);
        }

        return toolGroupsByName
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => new AgentToolGroup
            {
                Name = kvp.Key,
                Description = kvp.Value.Attr.Description,
                RequiredClaims = kvp.Value.Attr.RequiredClaims,
                Endpoints = kvp.Value.Endpoints
            })
            .ToList();
    }

    private AgentEndpointDetail BuildDetail(ApiDescription description)
    {
        var id = BuildId(description);

        var consumesTypes = description.SupportedRequestFormats
            .Select(f => f.MediaType)
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();

        var responseTypes = description.SupportedResponseTypes
            .GroupBy(r => r.StatusCode)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.SelectMany(r => r.ApiResponseFormats.Select(f => f.MediaType))
                      .Distinct()
                      .ToList());

        var parameters = description.ParameterDescriptions
            .Select(p => new AgentParameterInfo
            {
                Name = p.Name,
                Source = p.Source?.Id ?? string.Empty,
                Type = p.Type?.Name ?? string.Empty,
                IsRequired = p.IsRequired
            })
            .ToList();

        return new AgentEndpointDetail
        {
            Id = id,
            Name = BuildName(description),
            HttpMethod = (description.HttpMethod ?? "GET").ToUpperInvariant(),
            Route = $"/{(description.RelativePath ?? string.Empty).TrimStart('/')}",
            Description = BuildDescription(description),
            ConsumesContentTypes = consumesTypes,
            ResponseTypes = responseTypes,
            Parameters = parameters
        };
    }

    private static string NormalizeBasePath(string path)
    {
        var trimmed = path.TrimEnd('/');
        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }
}
