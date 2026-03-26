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
/// Test application that exercises <see cref="Configuration.AspeckdOptions.UseOpenApiMetadataFallback"/>.
/// Endpoints are annotated with standard OpenAPI metadata (<c>WithSummary</c>, <c>WithDescription</c>,
/// <c>WithName</c>) rather than the companion Aspeckd attributes, so that we can verify the
/// fallback resolution path.
/// </summary>
public sealed class FallbackTestWebAppFactory : WebApplicationFactory<FallbackTestWebAppFactory>
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
                        opt.Title = "Fallback Test API";
                        opt.UseOpenApiMetadataFallback = true;
                    });
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/products", () => Results.Ok("products"))
                            .WithName("GetProducts")
                            .WithSummary("Lists all products")
                            .WithDescription("Returns a complete list of available products.");

                        endpoints.MapGet("/api/categories", () => Results.Ok("categories"))
                            .WithName("GetCategories")
                            .WithSummary("Lists all categories");

                        endpoints.MapGet("/api/tags", () => Results.Ok("tags"))
                            .WithName("GetTags")
                            .WithDescription("Returns all tags in the system.");

                        endpoints.MapGet(
                            "/api/orders",
                            [AgentDescription("Agent-specific description")]
                            [AgentName("OrdersOverride")]
                            () => Results.Ok("orders"))
                            .WithName("GetOrders")
                            .WithSummary("Should not appear — agent attribute wins");

                        endpoints.MapGet("/api/bare", () => Results.Ok("bare"));

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(FallbackTestWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration((config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}
