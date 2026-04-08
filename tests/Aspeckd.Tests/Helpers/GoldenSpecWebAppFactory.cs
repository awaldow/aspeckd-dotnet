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
/// A comprehensive <see cref="WebApplicationFactory{TEntryPoint}"/> used exclusively by
/// <see cref="GoldenSpecTests"/> to exercise the full breadth of Aspeckd features in a
/// single golden-copy snapshot:
/// <list type="bullet">
///   <item>Multiple tool groups — one with <see cref="AgentToolGroupAttribute.RequiredClaims"/>, one without</item>
///   <item>Multiple HTTP verbs (GET, POST, DELETE) and a route-parameter endpoint</item>
///   <item>Request body and response-type metadata</item>
///   <item><see cref="AgentNameAttribute"/> and <see cref="AgentDescriptionAttribute"/> overrides</item>
///   <item>An <see cref="AgentExcludeAttribute"/> endpoint that must not appear in the spec</item>
///   <item>An ungrouped endpoint</item>
/// </list>
/// </summary>
public sealed class GoldenSpecWebAppFactory : WebApplicationFactory<GoldenSpecWebAppFactory>
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
                        opt.Title = "Golden Spec API";
                        opt.Description = "Comprehensive spec covering all Aspeckd features";
                    });
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // -------------------------------------------------------
                        // Weather group — two endpoints, description, required claims
                        // -------------------------------------------------------
                        endpoints.MapGet(
                            "/api/weather",
                            [AgentToolGroup("Weather",
                                Description = "Weather-related operations",
                                RequiredClaims = new string[] { "weather:read" })]
                            [AgentDescription("Get current weather")]
                            [AgentName("GetWeather")]
                            () => Results.Ok("weather"));

                        endpoints.MapGet(
                            "/api/weather/forecast",
                            [AgentToolGroup("Weather",
                                Description = "Weather-related operations",
                                RequiredClaims = new string[] { "weather:read" })]
                            [AgentDescription("Get weather forecast")]
                            [AgentName("GetWeatherForecast")]
                            () => Results.Ok("forecast"));

                        // -------------------------------------------------------
                        // Orders group — GET/POST/DELETE, no claims on the group,
                        // endpoint-level claims via AgentRequiredClaimsAttribute
                        // -------------------------------------------------------
                        endpoints.MapGet(
                            "/api/orders/{id}",
                            [AgentToolGroup("Orders", Description = "Order management operations")]
                            [AgentDescription("Get an order by its identifier")]
                            [AgentName("GetOrder")]
                            [AgentRequiredClaims("orders:read")]
                            (string id) => Results.Ok(id));

                        endpoints.MapPost(
                            "/api/orders",
                            [AgentToolGroup("Orders", Description = "Order management operations")]
                            [AgentDescription("Create a new order")]
                            [AgentName("CreateOrder")]
                            [AgentRequiredClaims("orders:write")]
                            () => Results.Created("/api/orders/1", null))
                            .Accepts<GoldenOrderRequest>("application/json");

                        endpoints.MapDelete(
                            "/api/orders/{id}",
                            [AgentToolGroup("Orders", Description = "Order management operations")]
                            [AgentDescription("Cancel an existing order")]
                            [AgentName("CancelOrder")]
                            [AgentRequiredClaims("orders:write")]
                            (string id) => Results.NoContent());

                        // -------------------------------------------------------
                        // Ungrouped — a plain health-check style endpoint
                        // -------------------------------------------------------
                        endpoints.MapGet(
                            "/api/status",
                            [AgentDescription("Returns the API health status")]
                            [AgentName("GetStatus")]
                            () => Results.Ok("ok"));

                        // -------------------------------------------------------
                        // Excluded — must NOT appear in the spec output
                        // -------------------------------------------------------
                        endpoints.MapGet(
                            "/api/internal",
                            [AgentExclude]
                            () => Results.Ok("internal"))
                            .WithName("InternalEndpoint");

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(GoldenSpecWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}

/// <summary>Request body type for the golden spec POST /api/orders endpoint.</summary>
public sealed class GoldenOrderRequest
{
    public string? ProductId { get; set; }
    public int Quantity { get; set; }
}
