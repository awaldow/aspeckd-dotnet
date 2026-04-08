using System.Text.Json.Serialization;

namespace Aspeckd.Models;

/// <summary>
/// Describes a single API version entry in the root version index.
/// Gives agents enough information to select the appropriate version and navigate to its tree.
/// </summary>
public sealed class AgentVersionInfo
{
    /// <summary>Version identifier string (e.g. <c>"v1"</c>, <c>"v2"</c>).</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Lifecycle status of this version.
    /// Common values: <c>"active"</c>, <c>"deprecated"</c>, <c>"preview"</c>.
    /// </summary>
    public string Status { get; init; } = "active";

    /// <summary>
    /// Optional ISO 8601 date string indicating when a deprecated version will be removed
    /// (e.g. <c>"2026-09-01"</c>).
    /// Gives agents a signal to warn users or prefer a newer version.
    /// Omitted from serialisation when <see langword="null"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SunsetDate { get; init; }

    /// <summary>
    /// URL of the full agent spec index for this version (e.g. <c>/.well-known/agents/v2</c>).
    /// </summary>
    public string IndexUrl { get; init; } = string.Empty;
}
