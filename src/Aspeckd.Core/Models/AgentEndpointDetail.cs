namespace Aspeckd.Models;

/// <summary>
/// Full detail for a single API endpoint, served at <c>/.well-known/agents/{id}</c>.
/// </summary>
public sealed class AgentEndpointDetail
{
    /// <summary>Stable identifier for the endpoint (e.g. <c>get-api-weather</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable name for the endpoint.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>HTTP method (GET, POST, PUT, DELETE, …).</summary>
    public string HttpMethod { get; init; } = string.Empty;

    /// <summary>Route template as registered in ASP.NET Core.</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>Agent-focused description, or the standard OpenAPI summary when available.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Authorization claim types required to call this endpoint, declared via
    /// <see cref="Aspeckd.Attributes.AgentRequiredClaimsAttribute"/>.
    /// Empty when no endpoint-level claim requirements are declared.
    /// </summary>
    public IReadOnlyList<string> RequiredClaims { get; init; } = [];

    /// <summary>
    /// Content types accepted by this endpoint (from <c>[Consumes]</c> metadata).
    /// Empty when the endpoint has no request body.
    /// </summary>
    public IReadOnlyList<string> ConsumesContentTypes { get; init; } = [];

    /// <summary>
    /// Possible response status codes and their associated content types.
    /// Key is the HTTP status code (e.g. <c>200</c>, <c>404</c>).
    /// </summary>
    public IReadOnlyDictionary<int, IReadOnlyList<string>> ResponseTypes { get; init; }
        = new Dictionary<int, IReadOnlyList<string>>();

    /// <summary>
    /// Parameters accepted by this endpoint (route, query, header).
    /// </summary>
    public IReadOnlyList<AgentParameterInfo> Parameters { get; init; } = [];
}
