using Aspeckd.Configuration;
using Aspeckd.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspeckd.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Aspeckd services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Aspeckd services required to serve the agent spec endpoints at runtime.
    /// The spec is generated dynamically from the application's API metadata on each request.
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
        services.AddHostedService<AgentSpecGeneratorHostedService>();

        return services;
    }

    /// <summary>
    /// Registers the Aspeckd services that serve the agent spec from a directory of
    /// pre-generated JSON files produced by <see cref="AgentSpecFileWriter"/> or the
    /// <c>GenerateAgentSpec</c> MSBuild target.
    /// </summary>
    /// <remarks>
    /// Use this overload in production when you want to serve the spec as static content
    /// (generated at build / publish time) instead of computing it dynamically on every
    /// request.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="staticFilesDirectory">
    /// Path to the directory that contains <c>index.json</c>, <c>schemas.json</c>, and
    /// the per-endpoint <c>{id}.json</c> files.
    /// </param>
    /// <param name="configure">Optional delegate to configure <see cref="AspeckdOptions"/>.</param>
    /// <returns>The <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddStaticAgentSpec(
        this IServiceCollection services,
        string staticFilesDirectory,
        Action<AspeckdOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staticFilesDirectory);

        var optionsBuilder = services.AddOptions<AspeckdOptions>();
        if (configure is not null)
            optionsBuilder.Configure(configure);

        services.AddSingleton<IAgentSpecProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AspeckdOptions>>().Value;
            var versionNames = options.Versions
                .Where(v => !string.IsNullOrWhiteSpace(v.Version))
                .Select(v => v.Version)
                .ToList();

            return versionNames.Count > 0
                ? new StaticFileAgentSpecProvider(staticFilesDirectory, versionNames)
                : new StaticFileAgentSpecProvider(staticFilesDirectory);
        });

        return services;
    }
}
