namespace Aspeckd.Services;

/// <summary>
/// Optional internal interface that <see cref="IAgentSpecProvider"/> implementations can
/// also implement to support efficient per-version access.
/// </summary>
/// <remarks>
/// <para>
/// When the route builder sees that the registered <see cref="IAgentSpecProvider"/> also
/// implements this interface it delegates per-version requests to the provider returned by
/// <see cref="GetVersionProvider"/>.  This allows the static-file provider to read from a
/// version-specific subdirectory rather than filtering the full spec at request time.
/// </para>
/// <para>
/// Implementations that do <em>not</em> implement this interface fall back to the default
/// route-layer filtering, which calls <see cref="IAgentSpecProvider.GetEndpointDetail"/>
/// for each endpoint to determine version membership via URL prefix matching.
/// </para>
/// </remarks>
internal interface IVersionedAgentSpecProviderFactory
{
    /// <summary>
    /// Returns a provider scoped to the given <paramref name="version"/>, or
    /// <see langword="null"/> when no version-specific provider is registered for that key.
    /// </summary>
    IAgentSpecProvider? GetVersionProvider(string version);
}
