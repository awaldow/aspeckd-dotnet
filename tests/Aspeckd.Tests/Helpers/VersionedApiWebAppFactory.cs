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

namespace Aspeckd.Tests.Helpers;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that registers a multi-version API
/// for testing the Aspeckd versioning feature.
/// <list type="bullet">
///   <item><c>v1</c> — active, URL prefix <c>api/v1</c></item>
///   <item><c>v2</c> — active (default), URL prefix <c>api/v2</c></item>
/// </list>
/// Endpoints:
/// <list type="bullet">
///   <item>GET /api/v1/weather — v1 only</item>
///   <item>GET /api/v2/weather — v2 only</item>
///   <item>GET /api/v2/forecast — v2 only</item>
///   <item>GET /api/status — ungrouped, no URL prefix → appears in both versions</item>
/// </list>
/// </summary>
public sealed class VersionedApiWebAppFactory : WebApplicationFactory<VersionedApiWebAppFactory>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAgentSpec(opt =>
                    {
                        opt.Title = "Versioned Test API";
                        opt.Description = "API with two active versions";
                        opt.DefaultVersion = "v2";
                        opt.Versions =
                        [
                            new AspeckdVersionOptions
                            {
                                Version = "v1",
                                Status = "deprecated",
                                SunsetDate = "2026-12-31",
                                UrlPrefix = "api/v1"
                            },
                            new AspeckdVersionOptions
                            {
                                Version = "v2",
                                Status = "active",
                                UrlPrefix = "api/v2"
                            }
                        ];
                    });
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // v1 endpoint
                        endpoints.MapGet(
                            "/api/v1/weather",
                            [AgentDescription("Get v1 weather")]
                            [AgentName("GetWeatherV1")]
                            () => Results.Ok("v1-weather"));

                        // v2 endpoints
                        endpoints.MapGet(
                            "/api/v2/weather",
                            [AgentDescription("Get v2 weather")]
                            [AgentName("GetWeatherV2")]
                            () => Results.Ok("v2-weather"));

                        endpoints.MapGet(
                            "/api/v2/forecast",
                            [AgentDescription("Get v2 forecast")]
                            [AgentName("GetForecastV2")]
                            () => Results.Ok("v2-forecast"));

                        // Ungrouped endpoint — no URL prefix → appears in all versions
                        endpoints.MapGet(
                            "/api/status",
                            [AgentDescription("API status")]
                            [AgentName("GetStatus")]
                            () => Results.Ok("ok"));

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(VersionedApiWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}
