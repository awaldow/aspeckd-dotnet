namespace Aspeckd.Models;

/// <summary>
/// The top-level agent spec index, served at <c>/agents</c>.
/// Analogous to an <c>llms.txt</c> file but structured as JSON and API-served.
/// </summary>
public sealed class AgentSpecIndex
{
    /// <summary>Title of the API, taken from <see cref="Configuration.AspeckdOptions.Title"/> or the app name.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Optional description of the API.</summary>
    public string? Description { get; init; }

    /// <summary>Relative URL to the schemas endpoint (e.g. <c>/agents/schemas</c>).</summary>
    public string SchemasUrl { get; init; } = string.Empty;

    /// <summary>
    /// Lean index entries for all non-excluded endpoints, ordered by HTTP method then route.
    /// Each entry carries only the name, description, and a <c>detailUrl</c> to the full
    /// endpoint document.
    /// </summary>
    public IReadOnlyList<AgentIndexEntry> Endpoints { get; init; } = [];

    /// <summary>
    /// Named tool groups declared via <see cref="Aspeckd.Attributes.AgentToolGroupAttribute"/>.
    /// Each group aggregates related endpoints and may carry a description and required
    /// authorization claims. Empty when no groups are defined.
    /// </summary>
    public IReadOnlyList<AgentToolGroup> Groups { get; init; } = [];
}
