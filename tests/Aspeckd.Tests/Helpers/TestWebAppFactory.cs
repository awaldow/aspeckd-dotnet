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
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> for a minimal in-memory ASP.NET Core app
/// that exercises the Aspeckd agent spec endpoints.
/// </summary>
public sealed class TestWebAppFactory : WebApplicationFactory<TestWebAppFactory>
{
    /// <summary>
    /// Optional base path override. Defaults to <c>/agents</c> when <see langword="null"/>.
    /// </summary>
    public string? AgentsBasePath { get; init; }

    protected override IHostBuilder CreateHostBuilder()
    {
        var agentsPath = AgentsBasePath;

        return Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();

                    if (agentsPath is not null)
                        services.AddAgentSpec(opt => opt.BasePath = agentsPath);
                    else
                        services.AddAgentSpec(opt =>
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
        // Pin the content root to the test binary's directory using host configuration.
        // CreateHost runs after WebApplicationFactory.ConfigureHostBuilder(), so this
        // ConfigureHostConfiguration callback is registered last and wins when the host config
        // is built — overriding the solution-relative path that WAF derived from the .sln file.
        var contentRoot = Path.GetDirectoryName(typeof(TestWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration((config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}

/// <summary>Placeholder request body type used in tests.</summary>
public sealed class SampleRequest
{
    public string? Name { get; set; }
}
