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
    /// <para><strong>Single-version (no <see cref="AspeckdOptions.Versions"/> configured):</strong></para>
    /// <list type="bullet">
    ///   <item><c>GET {basePath}</c> — spec index listing all endpoints</item>
    ///   <item><c>GET {basePath}/schemas</c> — all named schemas</item>
    ///   <item><c>GET {basePath}/{id}</c> — detail for a single endpoint</item>
    /// </list>
    /// <para><strong>Multi-version (<see cref="AspeckdOptions.Versions"/> is non-empty):</strong></para>
    /// <list type="bullet">
    ///   <item><c>GET {basePath}</c> — version index listing all API versions</item>
    ///   <item><c>GET {basePath}/{version}</c> — spec index scoped to that version</item>
    ///   <item><c>GET {basePath}/{version}/schemas</c> — schemas scoped to that version</item>
    ///   <item><c>GET {basePath}/{version}/{id}</c> — endpoint detail scoped to that version</item>
    /// </list>
    /// </summary>
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

        if (options.Versions.Count > 0)
        {
            MapVersionedRoutes(group, options, basePath);
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
        string basePath)
    {
        // GET {basePath} — version index
        group.MapGet("/", (IOptions<AspeckdOptions> opts) =>
        {
            var versionIndex = BuildVersionIndex(opts.Value, basePath);
            return Results.Ok(versionIndex);
        })
        .WithName("GetAgentVersionIndex")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();

        // GET {basePath}/{version} — spec index for a specific version
        group.MapGet("/{version}", (
            string version,
            IAgentSpecProvider provider,
            IOptions<AspeckdOptions> opts) =>
        {
            var versionConfig = FindVersionConfig(opts.Value, version);
            if (versionConfig is null)
                return Results.NotFound();

            var versionedBasePath = $"{basePath}/{version}";
            var versionProvider = ResolveVersionProvider(provider, version);
            var index = BuildVersionedIndex(versionProvider, versionConfig, opts.Value, versionedBasePath);
            return Results.Ok(index);
        })
        .WithName("GetVersionedAgentSpecIndex")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();

        // GET {basePath}/{version}/schemas — schemas for a specific version
        group.MapGet("/{version}/schemas", (
            string version,
            IAgentSpecProvider provider,
            IOptions<AspeckdOptions> opts) =>
        {
            var versionConfig = FindVersionConfig(opts.Value, version);
            if (versionConfig is null)
                return Results.NotFound();

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
            IAgentSpecProvider provider,
            IOptions<AspeckdOptions> opts) =>
        {
            var versionConfig = FindVersionConfig(opts.Value, version);
            if (versionConfig is null)
                return Results.NotFound();

            var versionProvider = ResolveVersionProvider(provider, version);
            var detail = versionProvider.GetEndpointDetail(id);
            if (detail is null)
                return Results.NotFound();

            // When a URL prefix is configured, validate this endpoint belongs to the version.
            if (!string.IsNullOrEmpty(versionConfig.UrlPrefix)
                && !AgentSpecFileWriter.RouteMatchesPrefix(detail.Route, versionConfig.UrlPrefix))
            {
                return Results.NotFound();
            }

            return Results.Ok(detail);
        })
        .WithName("GetVersionedAgentSpecEndpoint")
        .WithTags("AgentSpec")
        .ExcludeFromDescription();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static AspeckdVersionOptions? FindVersionConfig(AspeckdOptions options, string version)
        => options.Versions.FirstOrDefault(v =>
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

    private static AgentVersionIndex BuildVersionIndex(AspeckdOptions options, string basePath)
        => new()
        {
            ApiTitle = options.Title ?? "API",
            Description = options.Description,
            Versions = options.Versions
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
        var fullIndex = provider.GetIndex();
        return AgentSpecFileWriter.BuildFilteredIndex(provider, fullIndex, versionConfig, options, versionedBasePath);
    }
}

