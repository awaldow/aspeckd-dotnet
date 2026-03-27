using Aspeckd.Attributes;
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
/// Test application that exercises <see cref="AgentToolGroupAttribute"/>-based endpoint grouping.
/// </summary>
public sealed class ToolGroupTestWebAppFactory : WebApplicationFactory<ToolGroupTestWebAppFactory>
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
                        opt.Title = "Tool Group Test API";
                    });
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Two endpoints in the "Weather" group with claims and a description.
                        endpoints.MapGet(
                            "/api/weather",
                            [AgentToolGroup("Weather",
                                Description = "Weather-related operations",
                                RequiredClaims = new string[] { "weather:read" })]
                            [AgentDescription("Get current weather")]
                            () => Results.Ok("weather"));

                        endpoints.MapGet(
                            "/api/weather/forecast",
                            [AgentToolGroup("Weather",
                                Description = "Weather-related operations",
                                RequiredClaims = new string[] { "weather:read" })]
                            [AgentDescription("Get weather forecast")]
                            () => Results.Ok("forecast"));

                        // One endpoint in a separate "Inventory" group (no claims, no description).
                        endpoints.MapGet(
                            "/api/items",
                            [AgentToolGroup("Inventory")]
                            [AgentDescription("List all items")]
                            () => Results.Ok("items"));

                        // One endpoint with no group.
                        endpoints.MapGet("/api/ping", [AgentDescription("Ping")] () => Results.Ok("pong"));

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(ToolGroupTestWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration((config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}
