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

    /// <summary>All non-excluded endpoints, ordered by HTTP method then route.</summary>
    public IReadOnlyList<AgentEndpointSummary> Endpoints { get; init; } = [];
}
