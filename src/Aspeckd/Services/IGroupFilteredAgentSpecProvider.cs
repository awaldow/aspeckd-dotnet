using Aspeckd.Models;

namespace Aspeckd.Services;

/// <summary>
/// Optional internal interface that <see cref="IAgentSpecProvider"/> implementations can
/// also implement to support efficient per-API-description-group access.
/// </summary>
/// <remarks>
/// <para>
/// When the runtime provider (typically <see cref="AgentSpecProvider"/>) also implements
/// this interface and a version's <see cref="Configuration.AspeckdVersionOptions.GroupName"/>
/// is set, the route handlers delegate to the group-scoped methods here instead of applying
/// URL-prefix filtering.  This makes non-URL-segment versioning strategies (query-string,
/// header, etc.) work correctly without any <c>UrlPrefix</c> configuration.
/// </para>
/// <para>
/// Implementations that do <em>not</em> implement this interface fall back to the default
/// URL-prefix filtering, which matches routes against
/// <see cref="Configuration.AspeckdVersionOptions.UrlPrefix"/>.
/// </para>
/// </remarks>
internal interface IGroupFilteredAgentSpecProvider
{
    /// <summary>
    /// Returns the spec index containing only endpoints that belong to the given
    /// <paramref name="groupName"/> API-description group, with all detail URLs re-keyed
    /// to <paramref name="versionedBasePath"/>.
    /// </summary>
    AgentSpecIndex GetIndexForGroup(string groupName, string versionedBasePath);

    /// <summary>
    /// Returns the full detail for a single endpoint identified by <paramref name="id"/>
    /// that belongs to the given <paramref name="groupName"/>, or <see langword="null"/>
    /// when no matching endpoint exists in that group.
    /// </summary>
    AgentEndpointDetail? GetEndpointDetailForGroup(string id, string groupName);

    /// <summary>
    /// Returns all named schemas extracted from the endpoints that belong to the given
    /// <paramref name="groupName"/> API-description group.
    /// </summary>
    IReadOnlyList<AgentSchemaInfo> GetSchemasForGroup(string groupName);
}
