using Aspeckd.Attributes;
using Aspeckd.Configuration;
using Aspeckd.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspeckd.Tests.Helpers;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that captures log output and exposes
/// it for assertion in <see cref="DescriptionWarningTests"/>.
/// </summary>
/// <remarks>
/// Endpoints are configured so that various description-quality conditions are present:
/// <list type="bullet">
///   <item>One endpoint with a terse description (&lt; 30 chars): "Gets user" (9 chars) → ASPECKD001</item>
///   <item>One endpoint with no description at all → ASPECKD002</item>
///   <item>One endpoint with a long-enough description → no warning</item>
///   <item>One endpoint with a terse description but suppressed via <see cref="AspeckdSuppressWarningAttribute"/></item>
///   <item>One endpoint with no description but suppressed via <see cref="AspeckdSuppressWarningAttribute"/></item>
///   <item>One group with a terse description → ASPECKD001</item>
///   <item>One group with no description → ASPECKD002</item>
///   <item>One group with a suppressed terse description</item>
/// </list>
/// The root description is intentionally left as a terse string to trigger a root-level warning.
/// </remarks>
public sealed class DescriptionWarningTestWebAppFactory
    : WebApplicationFactory<DescriptionWarningTestWebAppFactory>
{
    private readonly IList<(LogLevel Level, string Message)> _logs;
    private readonly Action<AspeckdOptions>? _configure;

    public DescriptionWarningTestWebAppFactory(
        IList<(LogLevel Level, string Message)> logs,
        Action<AspeckdOptions>? configure = null)
    {
        _logs = logs;
        _configure = configure;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        var logs = _logs;
        var configure = _configure;

        return Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new CapturingLoggerProvider(logs));
                });
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAgentSpec(opt =>
                    {
                        opt.Title = "Warning Test API";
                        opt.Description = "Terse"; // 5 chars — triggers ASPECKD001 for root

                        configure?.Invoke(opt);
                    });
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // ASPECKD001: terse description (9 chars)
                        endpoints.MapGet("/api/user",
                            [AgentDescription("Gets user")]
                            [AgentName("GetUser")]
                            () => Results.Ok("user"));

                        // ASPECKD002: no description
                        endpoints.MapGet("/api/order",
                            [AgentName("GetOrder")]
                            () => Results.Ok("order"));

                        // OK: long-enough description (no warning)
                        endpoints.MapGet("/api/products",
                            [AgentDescription("Returns the full catalogue of available products including pricing and stock levels.")]
                            [AgentName("GetProducts")]
                            () => Results.Ok("products"));

                        // Suppressed ASPECKD001 (terse, but suppressed)
                        endpoints.MapGet("/api/health",
                            [AgentDescription("OK")]
                            [AgentName("HealthCheck")]
                            [AspeckdSuppressWarning("ASPECKD001")]
                            () => Results.Ok("ok"));

                        // Suppressed ASPECKD002 (no description, but suppressed for all codes)
                        endpoints.MapGet("/api/ping",
                            [AgentName("Ping")]
                            [AspeckdSuppressWarning]
                            () => Results.Ok("pong"));

                        // Group with a terse description → ASPECKD001
                        endpoints.MapGet("/api/widgets",
                            [AgentToolGroup("Widgets", Description = "Widgets")]
                            [AgentDescription("Returns the full catalogue of available widgets including pricing and stock levels.")]
                            [AgentName("GetWidgets")]
                            () => Results.Ok("widgets"));

                        // Group with no description → ASPECKD002
                        endpoints.MapGet("/api/gadgets",
                            [AgentToolGroup("Gadgets")]
                            [AgentDescription("Returns the full catalogue of available gadgets including pricing and stock levels.")]
                            [AgentName("GetGadgets")]
                            () => Results.Ok("gadgets"));

                        // Suppressed terse group description (suppression on representative endpoint)
                        endpoints.MapGet("/api/tools",
                            [AgentToolGroup("Tools", Description = "Tools")]
                            [AgentDescription("Returns the full catalogue of available tools including pricing and stock levels.")]
                            [AgentName("GetTools")]
                            [AspeckdSuppressWarning("ASPECKD001")]
                            () => Results.Ok("tools"));

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(DescriptionWarningTestWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}
