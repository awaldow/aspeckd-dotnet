using Asp.Versioning.ApiExplorer;
using Aspeckd.Configuration;
using Aspeckd.Models;
using Aspeckd.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspeckd.Extensions;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to map the agent spec routes.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the agent spec endpoints under the configured base path (default
    /// <c>/.well-known/agents</c>):
    /// <para><strong>Single-version (no <see cref="AspeckdOptions.Versions"/> configured and
    /// <c>IApiVersionDescriptionProvider</c> not available):</strong></para>
    /// <list type="bullet">
    ///   <item><c>GET {basePath}</c> — spec index listing all endpoints</item>
    ///   <item><c>GET {basePath}/schemas</c> — all named schemas</item>
    ///   <item><c>GET {basePath}/{id}</c> — detail for a single endpoint</item>
    /// </list>
    /// <para><strong>Multi-version (<see cref="AspeckdOptions.Versions"/> is non-empty, or
    /// versions are auto-detected from <c>IApiVersionDescriptionProvider</c>):</strong></para>
    /// <list type="bullet">
    ///   <item><c>GET {basePath}</c> — version index listing all API versions</item>
    ///   <item><c>GET {basePath}/{version}</c> — spec index scoped to that version</item>
    ///   <item><c>GET {basePath}/{version}/schemas</c> — schemas scoped to that version</item>
    ///   <item><c>GET {basePath}/{version}/{id}</c> — endpoint detail scoped to that version</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// When <c>Asp.Versioning</c> is installed and <c>AddApiVersioning()</c> +
    /// <c>AddApiExplorer()</c> have been called, Aspeckd will automatically discover all
    /// declared API versions from <c>IApiVersionDescriptionProvider</c> and activate the
    /// multi-version routing without any explicit <see cref="AspeckdOptions.Versions"/>
    /// configuration.  Explicitly configured versions always take precedence over auto-detected
    /// ones.
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>
    /// A <see cref="RouteGroupBuilder"/> representing the mapped group, allowing further
    /// customisation such as adding authorization policies.
    /// </returns>
    public static RouteGroupBuilder MapAgentSpec(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AspeckdOptions>>().Value;
        var basePath = options.BasePath.TrimEnd('/');
        if (!basePath.StartsWith('/'))
            basePath = $"/{basePath}";

        var group = endpoints.MapGroup(basePath);

        // Determine the effective version list.
        // Explicit configuration always wins; fall back to IApiVersionDescriptionProvider.
        var effectiveVersions = options.Versions.Count > 0
            ? (IReadOnlyList<AspeckdVersionOptions>)options.Versions
            : TryAutoDetectVersions(endpoints.ServiceProvider);

        if (effectiveVersions.Count > 0)
        {
            MapVersionedRoutes(group, options, effectiveVersions, basePath);
        }
        else
        {
            MapSingleVersionRoutes(group);
        }

        return group;
    }

    // -------------------------------------------------------------------------
    // Single-version routes (current / backward-compatible behaviour)
    // -------------------------------------------------------------------------

    private static void MapSingleVersionRoutes(RouteGroupBuilder group)
    {
        // GET {basePath} — spec index
        group.MapGet("/", (IAgentSpecProvider provider) =>
            Results.Ok(provider.GetIndex()))
            .WithName("GetAgentSpecIndex")
            .WithTags("AgentSpec")
            .ExcludeFromDescription();

        // GET {basePath}/schemas — all schemas
        group.MapGet("/schemas", (IAgentSpecProvider provider) =>
            Results.Ok(provider.GetSchemas()))
            .WithName("GetAgentSpecSchemas")
            .WithTags("AgentSpec")
            .ExcludeFromDescription();

        // GET {basePath}/{id} — single endpoint detail
        group.MapGet("/{id}", (string id, IAgentSpecProvider provider) =>
        {
            var detail = provider.GetEndpointDetail(id);
            return detail is not null ? Results.Ok(detail) : Results.NotFound();
        })
        .WithName("GetAgentSpecEndpoint")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();
    }

    // -------------------------------------------------------------------------
    // Multi-version routes
    // -------------------------------------------------------------------------

    private static void MapVersionedRoutes(
        RouteGroupBuilder group,
        AspeckdOptions options,
        IReadOnlyList<AspeckdVersionOptions> versions,
        string basePath)
    {
        // GET {basePath} — version index
        group.MapGet("/", () =>
        {
            var versionIndex = BuildVersionIndex(options, versions, basePath);
            return Results.Ok(versionIndex);
        })
        .WithName("GetAgentVersionIndex")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();

        // GET {basePath}/{version} — spec index for a specific version
        group.MapGet("/{version}", (
            string version,
            IAgentSpecProvider provider) =>
        {
            var versionConfig = FindVersionConfig(versions, version);
            if (versionConfig is null)
                return Results.NotFound();

            var versionedBasePath = $"{basePath}/{version}";

            // Group-name filtering: used when versions are auto-detected from Asp.Versioning.
            if (!string.IsNullOrEmpty(versionConfig.GroupName)
                && provider is IGroupFilteredAgentSpecProvider groupProvider)
            {
                var groupIndex = groupProvider.GetIndexForGroup(versionConfig.GroupName, versionedBasePath);
                return Results.Ok(new AgentSpecIndex
                {
                    Title = versionConfig.Title ?? options.Title ?? "API",
                    Description = versionConfig.Description ?? options.Description,
                    SchemasUrl = groupIndex.SchemasUrl,
                    Endpoints = groupIndex.Endpoints,
                    Groups = groupIndex.Groups,
                    Auth = options.Auth
                });
            }

            // URL-prefix / static-file path. ResolveVersionProvider directs static file
            // providers to the version-specific subdirectory.
            var versionProvider = ResolveVersionProvider(provider, version);
            var index = BuildVersionedIndex(versionProvider, versionConfig, options, versionedBasePath);
            return Results.Ok(index);
        })
        .WithName("GetVersionedAgentSpecIndex")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();

        // GET {basePath}/{version}/schemas — schemas for a specific version
        group.MapGet("/{version}/schemas", (
            string version,
            IAgentSpecProvider provider) =>
        {
            var versionConfig = FindVersionConfig(versions, version);
            if (versionConfig is null)
                return Results.NotFound();

            // When group-name filtering is available, scope schemas to the group.
            if (!string.IsNullOrEmpty(versionConfig.GroupName)
                && provider is IGroupFilteredAgentSpecProvider groupProvider)
            {
                return Results.Ok(groupProvider.GetSchemasForGroup(versionConfig.GroupName));
            }

            var versionProvider = ResolveVersionProvider(provider, version);
            return Results.Ok(versionProvider.GetSchemas());
        })
        .WithName("GetVersionedAgentSpecSchemas")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();

        // GET {basePath}/{version}/{id} — endpoint detail for a specific version
        group.MapGet("/{version}/{id}", (
            string version,
            string id,
            IAgentSpecProvider provider) =>
        {
            var versionConfig = FindVersionConfig(versions, version);
            if (versionConfig is null)
                return Results.NotFound();

            AgentEndpointDetail? detail;

            // When group-name filtering is available, scope the lookup to the group.
            if (!string.IsNullOrEmpty(versionConfig.GroupName)
                && provider is IGroupFilteredAgentSpecProvider groupProvider)
            {
                detail = groupProvider.GetEndpointDetailForGroup(id, versionConfig.GroupName);
            }
            else
            {
                var versionProvider = ResolveVersionProvider(provider, version);
                detail = versionProvider.GetEndpointDetail(id);

                // When a URL prefix is configured, validate this endpoint belongs to the version.
                if (detail is not null
                    && !string.IsNullOrEmpty(versionConfig.UrlPrefix)
                    && !AgentSpecFileWriter.RouteMatchesPrefix(detail.Route, versionConfig.UrlPrefix))
                {
                    detail = null;
                }
            }

            return detail is not null ? Results.Ok(detail) : Results.NotFound();
        })
        .WithName("GetVersionedAgentSpecEndpoint")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static AspeckdVersionOptions? FindVersionConfig(IReadOnlyList<AspeckdVersionOptions> versions, string version)
        => versions.FirstOrDefault(v =>
            string.Equals(v.Version, version, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns a version-scoped provider when the registered provider implements
    /// <see cref="IVersionedAgentSpecProviderFactory"/>.  Falls back to the main provider
    /// (dynamic mode — filtering is done in route helpers instead).
    /// </summary>
    private static IAgentSpecProvider ResolveVersionProvider(IAgentSpecProvider provider, string version)
        => provider is IVersionedAgentSpecProviderFactory factory
            ? factory.GetVersionProvider(version) ?? provider
            : provider;

    private static AgentVersionIndex BuildVersionIndex(
        AspeckdOptions options,
        IReadOnlyList<AspeckdVersionOptions> versions,
        string basePath)
        => new()
        {
            ApiTitle = options.Title ?? "API",
            Description = options.Description,
            Versions = versions
                .Select(v => new AgentVersionInfo
                {
                    Version = v.Version,
                    Status = v.Status,
                    SunsetDate = v.SunsetDate,
                    IndexUrl = $"{basePath}/{v.Version}"
                })
                .ToList(),
            DefaultVersion = options.DefaultVersion
        };

    private static AgentSpecIndex BuildVersionedIndex(
        IAgentSpecProvider provider,
        AspeckdVersionOptions versionConfig,
        AspeckdOptions options,
        string versionedBasePath)
    {
        // BuildFilteredIndex handles group-name filtering when versionConfig.GroupName is set
        // (used during static-file write operations).  For route-handler invocations, the
        // group-name path is taken before this method is called, so provider here is always
        // the URL-prefix / static-file provider.
        var fullIndex = provider.GetIndex();
        return AgentSpecFileWriter.BuildFilteredIndex(provider, fullIndex, versionConfig, options, versionedBasePath);
    }

    /// <summary>
    /// Attempts to auto-detect API versions from <c>IApiVersionDescriptionProvider</c>
    /// (from <c>Asp.Versioning</c>).  Returns an empty list when
    /// <c>IApiVersionDescriptionProvider</c> is not registered in the DI container.
    /// </summary>
    /// <remarks>
    /// Each auto-detected version has its <see cref="AspeckdVersionOptions.GroupName"/> set
    /// to the <c>ApiVersionDescription.GroupName</c>, enabling group-name-based endpoint
    /// filtering that works with all versioning strategies (URL segment, query-string, header).
    /// Deprecation status and sunset dates are read directly from <c>ApiVersionDescription</c>.
    /// </remarks>
    private static IReadOnlyList<AspeckdVersionOptions> TryAutoDetectVersions(IServiceProvider sp)
    {
        var provider = sp.GetService<IApiVersionDescriptionProvider>();
        if (provider is null)
            return [];

        return provider.ApiVersionDescriptions
            .OrderBy(d => d.GroupName, StringComparer.Ordinal)
            .Select(d => new AspeckdVersionOptions
            {
                Version = d.GroupName,
                Status = d.IsDeprecated ? "deprecated" : "active",
                SunsetDate = d.SunsetPolicy?.Date?.ToString("yyyy-MM-dd"),
                GroupName = d.GroupName
            })
            .ToList();
    }
}

