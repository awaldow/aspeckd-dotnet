using Aspeckd.Configuration;
using Aspeckd.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aspeckd.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Aspeckd services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Aspeckd services required to serve the agent spec endpoints.
    /// Call <c>app.MapAgentSpec()</c> after building the application to activate the routes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="AspeckdOptions"/>.</param>
    /// <returns>The <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAgentSpec(
        this IServiceCollection services,
        Action<AspeckdOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<AspeckdOptions>();
        if (configure is not null)
            optionsBuilder.Configure(configure);

        services.AddSingleton<IAgentSpecProvider, AgentSpecProvider>();

        return services;
    }
}
