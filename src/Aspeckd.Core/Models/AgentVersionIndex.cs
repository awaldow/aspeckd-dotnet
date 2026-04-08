using System.Text.Json.Serialization;

namespace Aspeckd.Models;

/// <summary>
/// The root-level response returned at <c>/.well-known/agents</c> when the API exposes
/// multiple versions.
/// </summary>
/// <remarks>
/// <para>
/// Detection logic for agents: if <c>versions</c> is present in the root response, the agent
/// follows the version index pattern and navigates to the per-version tree. If <c>versions</c>
/// is absent, the root response IS the index (the existing single-version behaviour).
/// </para>
/// <para>
/// Each entry in <see cref="Versions"/> links to a complete, self-contained Aspeckd doc tree
/// scoped to that version.
/// </para>
/// </remarks>
public sealed class AgentVersionIndex
{
    /// <summary>Title of the API.</summary>
    public string ApiTitle { get; init; } = string.Empty;

    /// <summary>
    /// Optional description of the API.
    /// Omitted from serialisation when <see langword="null"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// All available API versions. Each entry carries its lifecycle status and a link to
    /// the full doc tree for that version.
    /// </summary>
    public IReadOnlyList<AgentVersionInfo> Versions { get; init; } = [];

    /// <summary>
    /// The version identifier that agents should use when they have no specific version
    /// preference or context.
    /// Omitted from serialisation when <see langword="null"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultVersion { get; init; }
}
