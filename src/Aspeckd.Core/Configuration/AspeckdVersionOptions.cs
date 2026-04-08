namespace Aspeckd.Configuration;

/// <summary>
/// Configuration options for a single API version exposed through the Aspeckd versioned index.
/// </summary>
/// <remarks>
/// Add one or more instances of this class to <see cref="AspeckdOptions.Versions"/> to enable
/// the versioned root response at <c>/.well-known/agents</c>.
/// </remarks>
public sealed class AspeckdVersionOptions
{
    /// <summary>
    /// Version identifier string shown in the index and used as the URL segment for the
    /// version tree (e.g. <c>"v1"</c>, <c>"v2"</c>).
    /// The version's doc tree is served at <c>{BasePath}/{Version}</c>.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status of this version. Common values: <c>"active"</c>, <c>"deprecated"</c>,
    /// <c>"preview"</c>. Defaults to <c>"active"</c>.
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Optional ISO 8601 date string indicating when a deprecated version will be removed
    /// (e.g. <c>"2026-09-01"</c>). Signals to agents that they should prefer a newer version.
    /// Only meaningful when <see cref="Status"/> is <c>"deprecated"</c>.
    /// </summary>
    public string? SunsetDate { get; set; }

    /// <summary>
    /// Optional title override for this version. When <see langword="null"/>,
    /// <see cref="AspeckdOptions.Title"/> is used.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional description override for this version. When <see langword="null"/>,
    /// <see cref="AspeckdOptions.Description"/> is used.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional URL prefix used to filter which endpoints belong to this version
    /// (e.g. <c>"api/v1"</c> matches all endpoints whose route starts with <c>/api/v1/</c>).
    /// When <see langword="null"/> all endpoints are included in this version's tree.
    /// This supports URL segment versioning conventions such as <c>/api/v{version}/...</c>.
    /// </summary>
    public string? UrlPrefix { get; set; }
}
