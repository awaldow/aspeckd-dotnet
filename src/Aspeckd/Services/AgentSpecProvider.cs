using System.Text.RegularExpressions;
using System.Threading;
using Aspeckd.Attributes;
using Aspeckd.Configuration;
using Aspeckd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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
internal sealed class AgentSpecProvider : IAgentSpecProvider, IGroupFilteredAgentSpecProvider
{
    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionProvider;
    private readonly AspeckdOptions _options;
    private readonly ILogger<AgentSpecProvider> _logger;
    private int _warningsEmitted;

    public AgentSpecProvider(
        IApiDescriptionGroupCollectionProvider apiDescriptionProvider,
        IOptions<AspeckdOptions> options,
        ILogger<AgentSpecProvider> logger)
    {
        _apiDescriptionProvider = apiDescriptionProvider;
        _options = options.Value;
        _logger = logger;
    }

    public AgentSpecIndex GetIndex()
    {
        var basePath = NormalizeBasePath(_options.BasePath);
        var descriptions = GetVisibleDescriptions()
            .OrderBy(d => d.HttpMethod ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(d => d.RelativePath ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var endpoints = descriptions
            .Select(d => BuildIndexEntry(d, basePath))
            .ToList();

        var groups = BuildGroups(descriptions, basePath);

        // Emit description-quality warnings exactly once per provider lifetime.
        if (Interlocked.Exchange(ref _warningsEmitted, 1) == 0)
            EmitDescriptionWarnings(descriptions);

        return new AgentSpecIndex
        {
            Title = _options.Title ?? "API",
            Description = _options.Description,
            SchemasUrl = $"{basePath}/schemas",
            Endpoints = endpoints,
            Groups = groups,
            Auth = _options.Auth
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
    // IGroupFilteredAgentSpecProvider — group-name-scoped access (Asp.Versioning)
    // -------------------------------------------------------------------------

    AgentSpecIndex IGroupFilteredAgentSpecProvider.GetIndexForGroup(string groupName, string versionedBasePath)
    {
        var descriptions = GetVisibleDescriptionsForGroup(groupName)
            .OrderBy(d => d.HttpMethod ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(d => d.RelativePath ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var endpoints = descriptions
            .Select(d => BuildIndexEntry(d, versionedBasePath))
            .ToList();

        var groups = BuildGroups(descriptions, versionedBasePath);

        return new AgentSpecIndex
        {
            Title = _options.Title ?? "API",
            Description = _options.Description,
            SchemasUrl = $"{versionedBasePath}/schemas",
            Endpoints = endpoints,
            Groups = groups,
            Auth = _options.Auth
        };
    }

    AgentEndpointDetail? IGroupFilteredAgentSpecProvider.GetEndpointDetailForGroup(string id, string groupName)
    {
        var description = GetVisibleDescriptionsForGroup(groupName)
            .FirstOrDefault(d => BuildId(d) == id);
        return description is null ? null : BuildDetail(description);
    }

    IReadOnlyList<AgentSchemaInfo> IGroupFilteredAgentSpecProvider.GetSchemasForGroup(string groupName)
    {
        var types = new HashSet<Type>();
        foreach (var description in GetVisibleDescriptionsForGroup(groupName))
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

    private IEnumerable<ApiDescription> GetVisibleDescriptionsForGroup(string groupName)
    {
        return _apiDescriptionProvider.ApiDescriptionGroups.Items
            .Where(g => string.Equals(g.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
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

    private AgentIndexEntry BuildIndexEntry(ApiDescription description, string basePath)
    {
        var id = BuildId(description);

        return new AgentIndexEntry
        {
            Name = BuildName(description),
            Description = BuildDescription(description),
            DetailUrl = $"{basePath}/{id}"
        };
    }

    /// <summary>
    /// Builds the list of <see cref="AgentToolGroup"/> objects from the visible endpoint
    /// descriptions. Groups preserve the attribute's
    /// <see cref="AgentToolGroupAttribute.Description"/> and
    /// <see cref="AgentToolGroupAttribute.RequiredClaims"/> from the first endpoint seen
    /// for that group name (an implicit "representative" endpoint).
    /// </summary>
    private IReadOnlyList<AgentToolGroup> BuildGroups(
        IReadOnlyList<ApiDescription> descriptions,
        string basePath)
    {
        var toolGroupsByName = new Dictionary<string, (AgentToolGroupAttribute Attr, List<AgentIndexEntry> Endpoints)>(
            StringComparer.Ordinal);

        foreach (var d in descriptions)
        {
            var attr = d.ActionDescriptor?.EndpointMetadata
                ?.OfType<AgentToolGroupAttribute>().FirstOrDefault();
            if (attr is null)
                continue;

            var entry = BuildIndexEntry(d, basePath);

            if (!toolGroupsByName.TryGetValue(attr.Name, out var groupEntry))
            {
                groupEntry = (attr, []);
                toolGroupsByName[attr.Name] = groupEntry;
            }

            groupEntry.Endpoints.Add(entry);
        }

        return toolGroupsByName
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => new AgentToolGroup
            {
                Name = kvp.Key,
                Description = kvp.Value.Attr.Description,
                RequiredClaims = kvp.Value.Attr.RequiredClaims,
                Endpoints = kvp.Value.Endpoints,
                Auth = kvp.Value.Attr.AuthInstructions is not null
                    ? new AgentAuthInfo { Instructions = kvp.Value.Attr.AuthInstructions }
                    : null
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
            RequiredClaims = description.ActionDescriptor?.EndpointMetadata
                ?.OfType<AgentRequiredClaimsAttribute>().FirstOrDefault()?.Claims ?? [],
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

    // -------------------------------------------------------------------------
    // Description-quality warning helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Warning code for descriptions that are shorter than
    /// <see cref="AspeckdOptions.MinimumDescriptionLength"/>.
    /// </summary>
    internal const string WarnCodeTerse = "ASPECKD001";

    /// <summary>
    /// Warning code for descriptions that are <see langword="null"/> or empty.
    /// </summary>
    internal const string WarnCodeMissing = "ASPECKD002";

    private void EmitDescriptionWarnings(IReadOnlyList<ApiDescription> descriptions)
    {
        if (!_options.DescriptionWarnings)
            return;

        var minLen = _options.MinimumDescriptionLength;

        // ---------------------------------------------------------------
        // Root description
        // ---------------------------------------------------------------
        if (string.IsNullOrEmpty(_options.Description))
        {
            _logger.LogWarning(
                "{Code}: The root API description is missing. " +
                "Adding a root description helps agents understand the overall purpose of this API. " +
                "To suppress this warning, set options.DescriptionWarnings = false.",
                WarnCodeMissing);
        }
        else if (_options.Description.Length < minLen)
        {
            _logger.LogWarning(
                "{Code}: The root API description is {Length} character(s) (\"{Description}\"). " +
                "Descriptions under {MinLength} characters may be too terse for effective agent consumption. " +
                "Consider a description that explains what the API does, who its intended consumers are, " +
                "and any important usage notes. " +
                "To suppress this warning, set options.DescriptionWarnings = false.",
                WarnCodeTerse, _options.Description.Length, _options.Description, minLen);
        }

        // ---------------------------------------------------------------
        // Group descriptions — check representative endpoint per group
        // ---------------------------------------------------------------
        var groupRepresentatives = new Dictionary<string, (AgentToolGroupAttribute Attr, IList<object>? Metadata)>(
            StringComparer.Ordinal);

        foreach (var d in descriptions)
        {
            var attr = d.ActionDescriptor?.EndpointMetadata?.OfType<AgentToolGroupAttribute>().FirstOrDefault();
            if (attr is null)
                continue;

            if (!groupRepresentatives.ContainsKey(attr.Name))
                groupRepresentatives[attr.Name] = (attr, d.ActionDescriptor?.EndpointMetadata?.ToList());
        }

        foreach (var (groupName, (attr, metadata)) in groupRepresentatives)
        {
            if (string.IsNullOrEmpty(attr.Description))
            {
                if (!IsSuppressed(metadata, WarnCodeMissing))
                    _logger.LogWarning(
                        "{Code}: Group '{Group}' has no description. " +
                        "Adding a group description helps agents understand the shared purpose of these endpoints. " +
                        "To suppress this warning, set options.DescriptionWarnings = false or apply " +
                        "[AspeckdSuppressWarning] to the endpoint that declares the group.",
                        WarnCodeMissing, groupName);
            }
            else if (attr.Description.Length < minLen)
            {
                if (!IsSuppressed(metadata, WarnCodeTerse))
                    _logger.LogWarning(
                        "{Code}: Group '{Group}' has a description of {Length} character(s) (\"{Description}\"). " +
                        "Descriptions under {MinLength} characters may be too terse for effective agent consumption. " +
                        "Consider describing what this group of endpoints provides and when agents should use them. " +
                        "To suppress this warning, set options.DescriptionWarnings = false or apply " +
                        "[AspeckdSuppressWarning] to the endpoint that declares the group.",
                        WarnCodeTerse, groupName, attr.Description.Length, attr.Description, minLen);
            }
        }

        // ---------------------------------------------------------------
        // Endpoint descriptions
        // ---------------------------------------------------------------
        foreach (var d in descriptions)
        {
            var metadata = d.ActionDescriptor?.EndpointMetadata?.ToList();
            var desc = BuildDescription(d);
            var id = BuildId(d);

            if (string.IsNullOrEmpty(desc))
            {
                if (!IsSuppressed(metadata, WarnCodeMissing))
                    _logger.LogWarning(
                        "{Code}: Endpoint '{Id}' has no description. " +
                        "Agents rely on descriptions to understand what an endpoint does before calling it. " +
                        "Consider adding [AgentDescription(...)] with a description that explains what the endpoint " +
                        "returns, what parameters it expects, and notable behavior. " +
                        "To suppress this warning, set options.DescriptionWarnings = false or apply " +
                        "[AspeckdSuppressWarning] to the endpoint.",
                        WarnCodeMissing, id);
            }
            else if (desc.Length < minLen)
            {
                if (!IsSuppressed(metadata, WarnCodeTerse))
                    _logger.LogWarning(
                        "{Code}: Endpoint '{Id}' has a description of {Length} character(s) (\"{Description}\"). " +
                        "Descriptions under {MinLength} characters may be too terse for effective agent consumption. " +
                        "Consider describing what the endpoint returns, what parameters it expects, and notable behavior " +
                        "(e.g., \"Retrieve a user's full profile including role assignments and team memberships by UUID. " +
                        "Returns 404 if the user has been deleted.\"). " +
                        "To suppress this warning, set options.DescriptionWarnings = false or apply " +
                        "[AspeckdSuppressWarning] to the endpoint.",
                        WarnCodeTerse, id, desc.Length, desc, minLen);
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the endpoint metadata contains an
    /// <see cref="AspeckdSuppressWarningAttribute"/> that covers <paramref name="code"/>.
    /// An attribute with no codes suppresses all warnings.
    /// </summary>
    private static bool IsSuppressed(IList<object>? metadata, string code)
    {
        if (metadata is null)
            return false;

        return metadata.OfType<AspeckdSuppressWarningAttribute>()
            .Any(a => a.Codes.Length == 0 || a.Codes.Contains(code, StringComparer.Ordinal));
    }
}
