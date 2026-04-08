namespace Aspeckd.Models;

/// <summary>
/// A lean entry in the agent spec index representing a single API endpoint.
/// Carries just enough information for an agent to identify the endpoint and
/// navigate to its full detail page.
/// </summary>
/// <remarks>
/// Full endpoint information — HTTP method, route template, parameters, response
/// types — is available at the URL referenced by <see cref="DetailUrl"/>.
/// </remarks>
public sealed class AgentIndexEntry
{
    /// <summary>Human-readable name for the endpoint.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Agent-focused description, or <see langword="null"/> when none is available.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Relative URL to the full detail document for this endpoint
    /// (e.g. <c>/agents/get-api-weather</c>).
    /// </summary>
    public string DetailUrl { get; init; } = string.Empty;
}
