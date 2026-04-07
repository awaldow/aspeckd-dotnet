using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspeckd.Services;

/// <summary>
/// An <see cref="IHostedService"/> that generates the agent spec as static files and then
/// stops the host when the <c>ASPECKD_GENERATE</c> environment variable is set to
/// <c>1</c>.
/// </summary>
/// <remarks>
/// This service is automatically registered by <see cref="Extensions.ServiceCollectionExtensions.AddAgentSpec"/>
/// but is a no-op during normal application operation — it only activates when the
/// <c>ASPECKD_GENERATE</c> environment variable is present.
/// <para>
/// The <c>GenerateAgentSpec</c> MSBuild target (defined in <c>build/Aspeckd.targets</c>)
/// sets this variable when invoking the published application after <c>dotnet publish</c>,
/// causing the app to emit the static spec tree and exit immediately.
/// </para>
/// </remarks>
internal sealed class AgentSpecGeneratorHostedService : IHostedService
{
    /// <summary>
    /// Environment variable that activates build-time generation mode.
    /// When set to <c>1</c> the service generates the spec and stops the host.
    /// </summary>
    internal const string GenerateEnvVar = "ASPECKD_GENERATE";

    /// <summary>
    /// Environment variable that specifies the output directory for the generated files.
    /// Defaults to <c>wwwroot/agents</c> relative to the working directory when not set.
    /// </summary>
    internal const string OutputPathEnvVar = "ASPECKD_GENERATE_OUTPUT";

    private readonly IAgentSpecProvider _provider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AgentSpecGeneratorHostedService> _logger;

    public AgentSpecGeneratorHostedService(
        IAgentSpecProvider provider,
        IHostApplicationLifetime lifetime,
        ILogger<AgentSpecGeneratorHostedService> logger)
    {
        _provider = provider;
        _lifetime = lifetime;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var generate = Environment.GetEnvironmentVariable(GenerateEnvVar);
        if (!string.Equals(generate, "1", StringComparison.Ordinal))
            return;

        var outputPath = Environment.GetEnvironmentVariable(OutputPathEnvVar)
                         ?? Path.Combine("wwwroot", "agents");

        _logger.LogInformation("Aspeckd: generating static spec files to '{OutputPath}'…", outputPath);

        try
        {
            await AgentSpecFileWriter.WriteAsync(_provider, outputPath, cancellationToken: cancellationToken);
            _logger.LogInformation("Aspeckd: static spec generation complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aspeckd: static spec generation failed.");
            throw;
        }
        finally
        {
            // Signal the host to shut down so the build target process exits.
            _lifetime.StopApplication();
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
