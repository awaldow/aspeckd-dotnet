namespace Aspeckd.Models;

/// <summary>
/// Represents a named group of related API endpoints in the agent spec index.
/// Tool groups let agents discover related operations together and surface shared
/// context such as a human-readable description and required authorization claims.
/// </summary>
public sealed class AgentToolGroup
{
    /// <summary>Display name for this group (e.g. <c>"Weather"</c>).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional description that helps agents understand the purpose of this group.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Authorization claim types that must be present in a token/API key to call
    /// operations in this group. Agents can use this to contextualize auth failures
    /// (e.g. "your token is missing the <c>weather:read</c> claim").
    /// Empty when no claim requirements are declared.
    /// </summary>
    public IReadOnlyList<string> RequiredClaims { get; init; } = [];

    /// <summary>
    /// Lean index entries for all non-excluded endpoints that belong to this group,
    /// ordered by HTTP method then route.
    /// Full endpoint detail is available at each entry's <see cref="AgentIndexEntry.DetailUrl"/>.
    /// </summary>
    public IReadOnlyList<AgentIndexEntry> Endpoints { get; init; } = [];
}
