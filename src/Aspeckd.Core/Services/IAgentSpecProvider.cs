using Aspeckd.Models;

namespace Aspeckd.Services;

/// <summary>
/// Provides the agent spec index, individual endpoint details, and schema information
/// derived from the application's API metadata.
/// </summary>
public interface IAgentSpecProvider
{
    /// <summary>Returns the spec index listing all non-excluded endpoints.</summary>
    AgentSpecIndex GetIndex();

    /// <summary>
    /// Returns the full detail for a single endpoint identified by <paramref name="id"/>,
    /// or <see langword="null"/> when no matching endpoint exists.
    /// </summary>
    AgentEndpointDetail? GetEndpointDetail(string id);

    /// <summary>Returns all named schemas extracted from the API metadata.</summary>
    IReadOnlyList<AgentSchemaInfo> GetSchemas();
}
