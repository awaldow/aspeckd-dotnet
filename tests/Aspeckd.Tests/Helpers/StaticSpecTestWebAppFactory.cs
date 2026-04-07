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
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that wires up Aspeckd using
/// <see cref="ServiceCollectionExtensions.AddStaticAgentSpec"/> so that the
/// <c>/agents/*</c> endpoints are served from pre-generated JSON files stored in
/// <paramref name="staticFilesDirectory"/>.
/// </summary>
/// <remarks>
/// The test app registers the same routes as <see cref="TestWebAppFactory"/> so the
/// <c>GoldenSpec/</c> files produced by that factory can be used as the static spec
/// source, allowing an end-to-end verification that the static-file provider returns
/// identical data to the runtime provider.
/// </remarks>
public sealed class StaticSpecTestWebAppFactory : WebApplicationFactory<StaticSpecTestWebAppFactory>
{
    private readonly string _staticFilesDirectory;

    public StaticSpecTestWebAppFactory(string staticFilesDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staticFilesDirectory);
        _staticFilesDirectory = staticFilesDirectory;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        var dir = _staticFilesDirectory;

        return Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();

                    // Use the static provider backed by pre-generated JSON files.
                    services.AddStaticAgentSpec(
                        dir,
                        opt =>
                        {
                            opt.Title = "Test API";
                            opt.Description = "Integration test API";
                        });

                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Register the same application routes as TestWebAppFactory
                        // (not strictly needed for static serving but keeps the test
                        // app consistent).
                        endpoints.MapGet("/api/hello", [AgentDescription("Says hello")] [AgentName("Hello")] () =>
                            Results.Ok("hello"))
                            .WithName("Hello");

                        endpoints.MapGet("/api/hidden", [AgentExclude] () => Results.Ok("hidden"))
                            .WithName("Hidden");

                        endpoints.MapPost("/api/items", () => Results.Created("/api/items/1", null))
                            .WithName("CreateItem")
                            .Accepts<SampleRequest>("application/json");

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(StaticSpecTestWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}
