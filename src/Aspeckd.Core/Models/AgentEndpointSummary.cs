namespace Aspeckd.Models;

/// <summary>
/// Lightweight summary of a single API endpoint included in the agent spec index.
/// </summary>
public sealed class AgentEndpointSummary
{
    /// <summary>
    /// Stable identifier derived from the HTTP method and route template
    /// (e.g. <c>get-api-weather</c>). Used as the path segment in <c>/agents/{id}</c>.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable name for the endpoint. Supplied via <see cref="Aspeckd.Attributes.AgentNameAttribute"/>
    /// or derived automatically from the HTTP method and route.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>HTTP method (GET, POST, PUT, DELETE, …).</summary>
    public string HttpMethod { get; init; } = string.Empty;

    /// <summary>Route template as registered in ASP.NET Core (e.g. <c>/api/weather/{city}</c>).</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>
    /// Agent-focused description from <see cref="Aspeckd.Attributes.AgentDescriptionAttribute"/>,
    /// or the standard OpenAPI summary when available, or <see langword="null"/>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>Relative URL to the full detail page for this endpoint (e.g. <c>/agents/get-api-weather</c>).</summary>
    public string DetailUrl { get; init; } = string.Empty;

    /// <summary>
    /// Name of the <see cref="AgentToolGroup"/> this endpoint belongs to, or
    /// <see langword="null"/> when the endpoint is not part of any group.
    /// </summary>
    public string? Group { get; init; }
}
