namespace Aspeckd.Configuration;

/// <summary>
/// Configuration options for the Aspeckd agent spec endpoints.
/// </summary>
public sealed class AspeckdOptions
{
    /// <summary>
    /// The base path under which all agent spec endpoints are served.
    /// Defaults to <c>/.well-known/agents</c> (following the emerging convention for
    /// machine-readable agent discovery documents).
    /// <list type="bullet">
    ///   <item><c>{BasePath}</c> — spec index listing all endpoints</item>
    ///   <item><c>{BasePath}/{id}</c> — detail for a single endpoint</item>
    ///   <item><c>{BasePath}/schemas</c> — all response/request schemas</item>
    /// </list>
    /// </summary>
    public string BasePath { get; set; } = "/.well-known/agents";

    /// <summary>
    /// Optional title shown in the agent spec index.
    /// When <see langword="null"/> the application name is used.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional description shown at the top of the agent spec index.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the agent spec provider falls back to standard OpenAPI
    /// metadata — <c>WithSummary()</c> / <c>WithDescription()</c> / <c>WithName()</c> — for
    /// endpoints that do not carry <see cref="Attributes.AgentDescriptionAttribute"/> or
    /// <see cref="Attributes.AgentNameAttribute"/>.  This lets you reuse existing OpenAPI
    /// annotations instead of duplicating them with companion attributes.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseOpenApiMetadataFallback { get; set; } = false;
}
