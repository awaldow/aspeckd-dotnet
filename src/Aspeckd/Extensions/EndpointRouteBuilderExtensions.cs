using Aspeckd.Configuration;
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
    /// Maps the three agent spec endpoints under the configured base path (default <c>/agents</c>):
    /// <list type="bullet">
    ///   <item><c>GET {basePath}</c> — spec index listing all endpoints</item>
    ///   <item><c>GET {basePath}/schemas</c> — all named schemas</item>
    ///   <item><c>GET {basePath}/{id}</c> — detail for a single endpoint</item>
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

        return group;
    }
}
