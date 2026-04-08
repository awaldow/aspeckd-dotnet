using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Aspeckd.Attributes;
using Aspeckd.Configuration;
using Aspeckd.Extensions;
using Aspeckd.Models;
using Aspeckd.Services;
using Aspeckd.Tests.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspeckd.Tests;

/// <summary>
/// Integration tests for the API versioning feature.
/// Covers all acceptance criteria from the feature issue.
/// </summary>
public class ApiVersioningTests : IClassFixture<VersionedApiWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly VersionedApiWebAppFactory _factory;

    public ApiVersioningTests(VersionedApiWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // -----------------------------------------------------------------------
    // AC: Root endpoint returns AgentVersionIndex when versions are configured
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Root_ReturnsVersionIndex_WhenVersionsConfigured()
    {
        var response = await _client.GetAsync("/.well-known/agents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Must have a "versions" array — the distinguishing field for versioned root.
        Assert.True(doc.RootElement.TryGetProperty("versions", out var versions));
        Assert.Equal(JsonValueKind.Array, versions.ValueKind);
    }

    [Fact]
    public async Task Root_ContainsApiTitle()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Equal("Versioned Test API", index.ApiTitle);
    }

    [Fact]
    public async Task Root_ContainsDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Equal("API with two active versions", index.Description);
    }

    [Fact]
    public async Task Root_ContainsBothVersions()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Equal(2, index.Versions.Count);
        Assert.Contains(index.Versions, v => v.Version == "v1");
        Assert.Contains(index.Versions, v => v.Version == "v2");
    }

    [Fact]
    public async Task Root_VersionEntryIncludesRequiredFields()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var v1 = index.Versions.First(v => v.Version == "v1");
        Assert.Equal("deprecated", v1.Status);
        Assert.Equal("2026-12-31", v1.SunsetDate);
        Assert.Equal("/.well-known/agents/v1", v1.IndexUrl);

        var v2 = index.Versions.First(v => v.Version == "v2");
        Assert.Equal("active", v2.Status);
        Assert.Null(v2.SunsetDate);
        Assert.Equal("/.well-known/agents/v2", v2.IndexUrl);
    }

    [Fact]
    public async Task Root_ContainsDefaultVersion()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Equal("v2", index.DefaultVersion);
    }

    // -----------------------------------------------------------------------
    // AC: Each version's indexUrl returns a complete self-contained doc tree
    // -----------------------------------------------------------------------

    [Fact]
    public async Task V1Index_ReturnsOk()
    {
        var response = await _client.GetAsync("/.well-known/agents/v1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task V2Index_ReturnsOk()
    {
        var response = await _client.GetAsync("/.well-known/agents/v2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task V1Index_IsScopedToV1Endpoints()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v1");

        Assert.NotNull(index);
        // V1 endpoint must appear
        Assert.Contains(index.Endpoints, e => e.Name == "GetWeatherV1");
        // V2-only endpoints must NOT appear
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GetWeatherV2");
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GetForecastV2");
    }

    [Fact]
    public async Task V2Index_IsScopedToV2Endpoints()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v2");

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Name == "GetWeatherV2");
        Assert.Contains(index.Endpoints, e => e.Name == "GetForecastV2");
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GetWeatherV1");
    }

    [Fact]
    public async Task VersionIndex_SchemasUrlPointsToVersionedSchemasEndpoint()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v2");

        Assert.NotNull(index);
        Assert.Equal("/.well-known/agents/v2/schemas", index.SchemasUrl);
    }

    [Fact]
    public async Task VersionIndex_EndpointDetailUrlsAreVersionScoped()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v2");

        Assert.NotNull(index);
        Assert.All(index.Endpoints, e => Assert.StartsWith("/.well-known/agents/v2/", e.DetailUrl));
    }

    // -----------------------------------------------------------------------
    // AC: Version-scoped schemas endpoint
    // -----------------------------------------------------------------------

    [Fact]
    public async Task VersionedSchemas_ReturnsOk()
    {
        var response = await _client.GetAsync("/.well-known/agents/v2/schemas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionedSchemas_ReturnsList()
    {
        var schemas = await _client.GetFromJsonAsync<List<AgentSchemaInfo>>("/.well-known/agents/v2/schemas");
        Assert.NotNull(schemas);
    }

    // -----------------------------------------------------------------------
    // AC: Version-scoped endpoint detail
    // -----------------------------------------------------------------------

    [Fact]
    public async Task VersionedEndpointDetail_ReturnsOkForEndpointInVersion()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v2");
        Assert.NotNull(index);

        var weatherEntry = index.Endpoints.First(e => e.Name == "GetWeatherV2");
        var response = await _client.GetAsync(weatherEntry.DetailUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionedEndpointDetail_ReturnsCorrectData()
    {
        var detail = await _client.GetFromJsonAsync<AgentEndpointDetail>(
            "/.well-known/agents/v2/get-api-v2-weather");

        Assert.NotNull(detail);
        Assert.Equal("GetWeatherV2", detail.Name);
        Assert.Equal("GET", detail.HttpMethod);
        Assert.Equal("/api/v2/weather", detail.Route);
    }

    [Fact]
    public async Task VersionedEndpointDetail_ReturnsNotFoundForEndpointOutsideVersion()
    {
        // get-api-v1-weather belongs to v1; requesting it under v2 should return 404.
        var response = await _client.GetAsync("/.well-known/agents/v2/get-api-v1-weather");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VersionedEndpointDetail_ReturnsNotFoundForUnknownId()
    {
        var response = await _client.GetAsync("/.well-known/agents/v2/does-not-exist-xyz");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // AC: Unknown version returns 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UnknownVersionIndex_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/.well-known/agents/v99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownVersionSchemas_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/.well-known/agents/v99/schemas");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownVersionEndpointDetail_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/.well-known/agents/v99/get-api-v2-weather");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // AC: Agent can distinguish versioned root from a direct index
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Root_HasVersionsFieldThatIdentifiesVersionedResponse()
    {
        var json = await _client.GetStringAsync("/.well-known/agents");
        using var doc = JsonDocument.Parse(json);

        // Versioned root has "versions"; non-versioned root has "endpoints".
        Assert.True(doc.RootElement.TryGetProperty("versions", out _),
            "Versioned root should have 'versions' property.");
        Assert.False(doc.RootElement.TryGetProperty("endpoints", out _),
            "Versioned root should NOT have 'endpoints' property (that belongs to AgentSpecIndex).");
    }

    // -----------------------------------------------------------------------
    // AC: Single-version APIs continue to work (no versions wrapper)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SingleVersionApi_RootReturnsAgentSpecIndex_NotVersionIndex()
    {
        await using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var json = await client.GetStringAsync("/.well-known/agents");
        using var doc = JsonDocument.Parse(json);

        // Non-versioned root has "endpoints"; versioned root has "versions".
        Assert.True(doc.RootElement.TryGetProperty("endpoints", out _),
            "Single-version root should have 'endpoints' property (AgentSpecIndex).");
        Assert.False(doc.RootElement.TryGetProperty("versions", out _),
            "Single-version root should NOT have 'versions' property.");
    }
}

// -----------------------------------------------------------------------
// AspeckdOptions versioning configuration tests
// -----------------------------------------------------------------------

public class AspeckdVersionOptionsTests
{
    [Fact]
    public void DefaultStatus_IsActive()
    {
        var opts = new AspeckdVersionOptions { Version = "v1" };
        Assert.Equal("active", opts.Status);
    }

    [Fact]
    public void SunsetDate_CanBeSet()
    {
        var opts = new AspeckdVersionOptions { Version = "v1", SunsetDate = "2026-09-01" };
        Assert.Equal("2026-09-01", opts.SunsetDate);
    }

    [Fact]
    public void UrlPrefix_CanBeSet()
    {
        var opts = new AspeckdVersionOptions { Version = "v1", UrlPrefix = "api/v1" };
        Assert.Equal("api/v1", opts.UrlPrefix);
    }

    [Fact]
    public void AspeckdOptions_DefaultVersions_IsEmpty()
    {
        var options = new AspeckdOptions();
        Assert.Empty(options.Versions);
    }

    [Fact]
    public void AspeckdOptions_DefaultDefaultVersion_IsNull()
    {
        var options = new AspeckdOptions();
        Assert.Null(options.DefaultVersion);
    }

    [Fact]
    public void AspeckdOptions_Versions_CanBeSet()
    {
        var options = new AspeckdOptions
        {
            Versions = [new AspeckdVersionOptions { Version = "v1" }],
            DefaultVersion = "v1"
        };
        Assert.Single(options.Versions);
        Assert.Equal("v1", options.DefaultVersion);
    }
}

// -----------------------------------------------------------------------
// AgentSpecFileWriter versioned output tests
// -----------------------------------------------------------------------

public class VersionedFileWriterTests : IClassFixture<VersionedApiWebAppFactory>, IAsyncLifetime
{
    private readonly IAgentSpecProvider _provider;
    private readonly VersionedApiWebAppFactory _factory;
    private string? _tempDir;

    public VersionedFileWriterTests(VersionedApiWebAppFactory factory)
    {
        _factory = factory;
        _provider = factory.Services.GetRequiredService<IAgentSpecProvider>();
    }

    public async Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aspeckd-versioned-{Guid.NewGuid():N}");

        var options = new AspeckdOptions
        {
            Title = "Versioned Test API",
            Description = "API with two active versions",
            DefaultVersion = "v2",
            Versions =
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
            ]
        };

        await AgentSpecFileWriter.WriteAsync(_provider, _tempDir, options);
    }

    public Task DisposeAsync()
    {
        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public void WriteVersioned_CreatesRootIndexAsVersionIndex()
    {
        var path = Path.Combine(_tempDir!, "index.json");
        Assert.True(File.Exists(path), "Root index.json should be written.");

        var content = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(content);

        // Must have "versions" field (AgentVersionIndex), not "endpoints" (AgentSpecIndex).
        Assert.True(doc.RootElement.TryGetProperty("versions", out _));
        Assert.False(doc.RootElement.TryGetProperty("endpoints", out _));
    }

    [Fact]
    public void WriteVersioned_CreatesV1Subdirectory()
    {
        Assert.True(Directory.Exists(Path.Combine(_tempDir!, "v1")),
            "v1 subdirectory should be created.");
        Assert.True(File.Exists(Path.Combine(_tempDir!, "v1", "index.json")),
            "v1/index.json should be written.");
        Assert.True(File.Exists(Path.Combine(_tempDir!, "v1", "schemas.json")),
            "v1/schemas.json should be written.");
    }

    [Fact]
    public void WriteVersioned_CreatesV2Subdirectory()
    {
        Assert.True(Directory.Exists(Path.Combine(_tempDir!, "v2")),
            "v2 subdirectory should be created.");
        Assert.True(File.Exists(Path.Combine(_tempDir!, "v2", "index.json")),
            "v2/index.json should be written.");
        Assert.True(File.Exists(Path.Combine(_tempDir!, "v2", "schemas.json")),
            "v2/schemas.json should be written.");
    }

    [Fact]
    public void WriteVersioned_V1IndexContainsOnlyV1Endpoints()
    {
        var content = File.ReadAllText(Path.Combine(_tempDir!, "v1", "index.json"));
        var index = JsonSerializer.Deserialize<AgentSpecIndex>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Name == "GetWeatherV1");
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GetWeatherV2");
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GetForecastV2");
    }

    [Fact]
    public void WriteVersioned_V2IndexContainsOnlyV2Endpoints()
    {
        var content = File.ReadAllText(Path.Combine(_tempDir!, "v2", "index.json"));
        var index = JsonSerializer.Deserialize<AgentSpecIndex>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Name == "GetWeatherV2");
        Assert.Contains(index.Endpoints, e => e.Name == "GetForecastV2");
        Assert.DoesNotContain(index.Endpoints, e => e.Name == "GetWeatherV1");
    }

    [Fact]
    public void WriteVersioned_V2IndexSchemasUrlPointsToV2Schemas()
    {
        var content = File.ReadAllText(Path.Combine(_tempDir!, "v2", "index.json"));
        var index = JsonSerializer.Deserialize<AgentSpecIndex>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(index);
        Assert.Equal("/.well-known/agents/v2/schemas", index.SchemasUrl);
    }

    [Fact]
    public void WriteVersioned_RootIndexContainsDefaultVersion()
    {
        var content = File.ReadAllText(Path.Combine(_tempDir!, "index.json"));
        var versionIndex = JsonSerializer.Deserialize<AgentVersionIndex>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(versionIndex);
        Assert.Equal("v2", versionIndex.DefaultVersion);
    }

    [Fact]
    public void WriteVersioned_RootIndexContainsSunsetDateForV1()
    {
        var content = File.ReadAllText(Path.Combine(_tempDir!, "index.json"));
        var versionIndex = JsonSerializer.Deserialize<AgentVersionIndex>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(versionIndex);
        var v1 = versionIndex.Versions.First(v => v.Version == "v1");
        Assert.Equal("2026-12-31", v1.SunsetDate);
    }
}

// -----------------------------------------------------------------------
// Static versioned file provider tests
// -----------------------------------------------------------------------

public class VersionedStaticProviderTests : IAsyncLifetime
{
    private string? _tempDir;
    private StaticVersionedWebAppFactory? _staticFactory;
    private HttpClient? _staticClient;

    public async Task InitializeAsync()
    {
        // Generate spec into a temp directory using the dynamic provider.
        _tempDir = Path.Combine(Path.GetTempPath(), $"aspeckd-static-v-{Guid.NewGuid():N}");

        await using var dynamicFactory = new VersionedApiWebAppFactory();
        var dynamicProvider = dynamicFactory.Services.GetRequiredService<IAgentSpecProvider>();

        var options = new AspeckdOptions
        {
            Title = "Versioned Test API",
            DefaultVersion = "v2",
            Versions =
            [
                new AspeckdVersionOptions { Version = "v1", Status = "active", UrlPrefix = "api/v1" },
                new AspeckdVersionOptions { Version = "v2", Status = "active", UrlPrefix = "api/v2" }
            ]
        };

        await AgentSpecFileWriter.WriteAsync(dynamicProvider, _tempDir, options);

        // Spin up a static factory backed by those files.
        _staticFactory = new StaticVersionedWebAppFactory(_tempDir);
        _staticClient = _staticFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_staticFactory is not null)
            await _staticFactory.DisposeAsync();

        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task Static_V2Index_ReturnsOk()
    {
        var response = await _staticClient!.GetAsync("/.well-known/agents/v2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Static_V2Index_ContainsV2Endpoints()
    {
        var index = await _staticClient!.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v2");

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Name == "GetWeatherV2");
    }

    [Fact]
    public async Task Static_V1Index_ContainsV1Endpoints()
    {
        var index = await _staticClient!.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents/v1");

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Name == "GetWeatherV1");
    }

    [Fact]
    public async Task Static_UnknownVersion_ReturnsNotFound()
    {
        var response = await _staticClient!.GetAsync("/.well-known/agents/v99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>Helper factory for static versioned spec tests.</summary>
internal sealed class StaticVersionedWebAppFactory
    : WebApplicationFactory<StaticVersionedWebAppFactory>
{
    private readonly string _staticFilesDirectory;

    public StaticVersionedWebAppFactory(string staticFilesDirectory)
    {
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
                    services.AddStaticAgentSpec(dir, opt =>
                    {
                        opt.Title = "Versioned Test API";
                        opt.DefaultVersion = "v2";
                        opt.Versions =
                        [
                            new AspeckdVersionOptions { Version = "v1", Status = "active", UrlPrefix = "api/v1" },
                            new AspeckdVersionOptions { Version = "v2", Status = "active", UrlPrefix = "api/v2" }
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
                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(StaticVersionedWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));
        return base.CreateHost(builder);
    }
}

// -----------------------------------------------------------------------
// Auto-detection of versions from IApiVersionDescriptionProvider
// -----------------------------------------------------------------------

/// <summary>
/// Integration tests verifying that <c>MapAgentSpec()</c> auto-detects API versions
/// from <c>IApiVersionDescriptionProvider</c> when no explicit
/// <see cref="AspeckdVersionOptions"/> are configured.
/// </summary>
public class ApiVersionAutoDetectionTests : IClassFixture<AutoDetectedVersionWebAppFactory>
{
    private readonly HttpClient _client;

    public ApiVersionAutoDetectionTests(AutoDetectedVersionWebAppFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task AutoDetect_Root_ReturnsVersionIndex()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Equal(2, index.Versions.Count);
    }

    [Fact]
    public async Task AutoDetect_Versions_HaveCorrectStatus()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var v1 = index.Versions.First(v => v.Version == "v1");
        var v2 = index.Versions.First(v => v.Version == "v2");
        Assert.Equal("deprecated", v1.Status);
        Assert.Equal("active", v2.Status);
    }

    [Fact]
    public async Task AutoDetect_SunsetDate_IsForwardedFromSunsetPolicy()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var v1 = index.Versions.First(v => v.Version == "v1");
        Assert.Equal("2026-09-01", v1.SunsetDate);
    }

    [Fact]
    public async Task AutoDetect_IndexUrl_PointsToVersionedPath()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.All(index.Versions, v =>
            Assert.Equal($"/.well-known/agents/{v.Version}", v.IndexUrl));
    }

    [Fact]
    public async Task AutoDetect_VersionedRoute_Returns200()
    {
        var v1Response = await _client.GetAsync("/.well-known/agents/v1");
        var v2Response = await _client.GetAsync("/.well-known/agents/v2");

        Assert.Equal(HttpStatusCode.OK, v1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, v2Response.StatusCode);
    }

    [Fact]
    public async Task AutoDetect_UnknownVersion_Returns404()
    {
        var response = await _client.GetAsync("/.well-known/agents/v99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Integration tests verifying that explicitly configured
/// <see cref="AspeckdVersionOptions.Versions"/> take precedence over auto-detected versions
/// from <c>IApiVersionDescriptionProvider</c>.
/// </summary>
public class ExplicitVersionsPrecedenceTests : IClassFixture<ExplicitVersionsPrecedenceWebAppFactory>
{
    private readonly HttpClient _client;

    public ExplicitVersionsPrecedenceTests(ExplicitVersionsPrecedenceWebAppFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task ExplicitVersions_TakePrecedenceOverAutoDetected()
    {
        var index = await _client.GetFromJsonAsync<AgentVersionIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // Only the explicitly configured version "v3" should appear, NOT "v1" or "v2" from IApiVersionDescriptionProvider.
        Assert.Single(index.Versions);
        Assert.Equal("v3", index.Versions[0].Version);
    }
}

/// <summary>
/// Web app factory that registers a stub <see cref="IApiVersionDescriptionProvider"/> with
/// two versions (v1 deprecated with sunset date, v2 active) but does NOT set
/// <see cref="AspeckdOptions.Versions"/> explicitly, so auto-detection kicks in.
/// </summary>
public sealed class AutoDetectedVersionWebAppFactory
    : WebApplicationFactory<AutoDetectedVersionWebAppFactory>
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
                    services.AddAgentSpec(opt => opt.Title = "Auto-Detected API");

                    // Register a stub IApiVersionDescriptionProvider — NO opt.Versions configured.
                    services.AddSingleton<IApiVersionDescriptionProvider>(
                        new StubApiVersionDescriptionProvider(
                        [
                            new ApiVersionDescription(
                                new ApiVersion(1, 0),
                                "v1",
                                deprecated: true,
                                new SunsetPolicy(
                                    new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero))),
                            new ApiVersionDescription(
                                new ApiVersion(2, 0),
                                "v2",
                                deprecated: false)
                        ]));

                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // A v1 endpoint (will be in default API description group).
                        // We register it under a route that the group-name filter would match
                        // if Asp.Versioning were fully wired, but here we just verify HTTP responses.
                        endpoints.MapGet(
                            "/api/v1/weather",
                            [AgentDescription("Get v1 weather")]
                            [AgentName("GetWeatherV1")]
                            () => Results.Ok("v1-weather"));

                        endpoints.MapAgentSpec();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(AutoDetectedVersionWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));
        return base.CreateHost(builder);
    }
}

/// <summary>
/// Web app factory that registers BOTH a stub <see cref="IApiVersionDescriptionProvider"/>
/// (v1, v2) AND explicit <see cref="AspeckdOptions.Versions"/> (v3).  Used to verify that
/// explicit configuration always wins.
/// </summary>
public sealed class ExplicitVersionsPrecedenceWebAppFactory
    : WebApplicationFactory<ExplicitVersionsPrecedenceWebAppFactory>
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
                        opt.Title = "Precedence Test API";
                        // Explicit versions — should win over IApiVersionDescriptionProvider.
                        opt.Versions =
                        [
                            new AspeckdVersionOptions { Version = "v3", Status = "active" }
                        ];
                    });

                    // Also register a stub provider that returns v1 and v2.
                    services.AddSingleton<IApiVersionDescriptionProvider>(
                        new StubApiVersionDescriptionProvider(
                        [
                            new ApiVersionDescription(
                                new ApiVersion(1, 0), "v1", false),
                            new ApiVersionDescription(
                                new ApiVersion(2, 0), "v2", false)
                        ]));

                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapAgentSpec());
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(ExplicitVersionsPrecedenceWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));
        return base.CreateHost(builder);
    }
}

/// <summary>
/// Minimal stub implementation of <see cref="IApiVersionDescriptionProvider"/> for testing
/// the auto-detection path in <c>MapAgentSpec()</c>.
/// </summary>
internal sealed class StubApiVersionDescriptionProvider : IApiVersionDescriptionProvider
{
    public StubApiVersionDescriptionProvider(
        IReadOnlyList<ApiVersionDescription> descriptions)
        => ApiVersionDescriptions = descriptions;

    public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions { get; }
}
