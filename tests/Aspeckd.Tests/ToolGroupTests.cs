using System.Net.Http.Json;
using Aspeckd.Models;
using Aspeckd.Tests.Helpers;

namespace Aspeckd.Tests;

public class ToolGroupTests : IClassFixture<ToolGroupTestWebAppFactory>
{
    private readonly HttpClient _client;

    public ToolGroupTests(ToolGroupTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -----------------------------------------------------------------------
    // Groups are included in the index response
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_IncludesGroups()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.NotEmpty(index.Groups);
    }

    [Fact]
    public async Task GetIndex_GroupsContainWeatherGroup()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var weather = index.Groups.FirstOrDefault(g => g.Name == "Weather");
        Assert.NotNull(weather);
    }

    [Fact]
    public async Task GetIndex_WeatherGroupHasCorrectDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Equal("Weather-related operations", weather.Description);
    }

    [Fact]
    public async Task GetIndex_WeatherGroupHasRequiredClaims()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Contains("weather:read", weather.RequiredClaims);
    }

    [Fact]
    public async Task GetIndex_WeatherGroupContainsBothWeatherEndpoints()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Contains(weather.Endpoints, e => e.DetailUrl.Contains("get-api-weather") && !e.DetailUrl.Contains("forecast"));
        Assert.Contains(weather.Endpoints, e => e.DetailUrl.Contains("get-api-weather-forecast"));
    }

    [Fact]
    public async Task GetIndex_InventoryGroupHasNoRequiredClaims()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var inventory = index.Groups.First(g => g.Name == "Inventory");
        Assert.Empty(inventory.RequiredClaims);
    }

    [Fact]
    public async Task GetIndex_InventoryGroupHasNullDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var inventory = index.Groups.First(g => g.Name == "Inventory");
        Assert.Null(inventory.Description);
    }

    // -----------------------------------------------------------------------
    // Endpoint group membership is reflected in the groups list
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_GroupedEndpointAppearsInGroupEndpointsList()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // /api/weather should appear in the Weather group's Endpoints list.
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Contains(weather.Endpoints, e => e.DetailUrl.Contains("get-api-weather") && !e.DetailUrl.Contains("forecast"));
    }

    [Fact]
    public async Task GetIndex_UngroupedEndpointDoesNotAppearInAnyGroup()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        // /api/ping has no AgentToolGroup attribute and must not appear inside any group.
        Assert.All(index.Groups, g => Assert.DoesNotContain(g.Endpoints, e => e.Name == "GET /api/ping"));
    }

    // -----------------------------------------------------------------------
    // Flat endpoints list still includes all endpoints
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_FlatEndpointsListIsUnaffectedByGroups()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Name == "GET /api/weather");
        Assert.Contains(index.Endpoints, e => e.DetailUrl.EndsWith("get-api-weather-forecast"));
        Assert.Contains(index.Endpoints, e => e.Name == "GET /api/items");
        Assert.Contains(index.Endpoints, e => e.Name == "GET /api/ping");
    }

    // -----------------------------------------------------------------------
    // No groups when none are defined
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_GroupsEmptyWhenNoGroupsDefined()
    {
        // The default TestWebAppFactory does not use any AgentToolGroupAttribute.
        await using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Empty(index.Groups);
    }
}
