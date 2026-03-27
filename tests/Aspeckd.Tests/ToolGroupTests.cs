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
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.NotEmpty(index.Groups);
    }

    [Fact]
    public async Task GetIndex_GroupsContainWeatherGroup()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var weather = index.Groups.FirstOrDefault(g => g.Name == "Weather");
        Assert.NotNull(weather);
    }

    [Fact]
    public async Task GetIndex_WeatherGroupHasCorrectDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Equal("Weather-related operations", weather.Description);
    }

    [Fact]
    public async Task GetIndex_WeatherGroupHasRequiredClaims()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Contains("weather:read", weather.RequiredClaims);
    }

    [Fact]
    public async Task GetIndex_WeatherGroupContainsBothWeatherEndpoints()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var weather = index.Groups.First(g => g.Name == "Weather");
        Assert.Contains(weather.Endpoints, e => e.Route == "/api/weather");
        Assert.Contains(weather.Endpoints, e => e.Route == "/api/weather/forecast");
    }

    [Fact]
    public async Task GetIndex_InventoryGroupHasNoRequiredClaims()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var inventory = index.Groups.First(g => g.Name == "Inventory");
        Assert.Empty(inventory.RequiredClaims);
    }

    [Fact]
    public async Task GetIndex_InventoryGroupHasNullDescription()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var inventory = index.Groups.First(g => g.Name == "Inventory");
        Assert.Null(inventory.Description);
    }

    // -----------------------------------------------------------------------
    // Endpoint summaries carry the group name
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_GroupedEndpointSummaryCarriesGroupName()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var weatherEndpoint = index.Endpoints.FirstOrDefault(e => e.Route == "/api/weather");
        Assert.NotNull(weatherEndpoint);
        Assert.Equal("Weather", weatherEndpoint.Group);
    }

    [Fact]
    public async Task GetIndex_UngroupedEndpointSummaryHasNullGroup()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        var ping = index.Endpoints.FirstOrDefault(e => e.Route == "/api/ping");
        Assert.NotNull(ping);
        Assert.Null(ping.Group);
    }

    // -----------------------------------------------------------------------
    // Flat endpoints list still includes all endpoints
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_FlatEndpointsListIsUnaffectedByGroups()
    {
        var index = await _client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.Contains(index.Endpoints, e => e.Route == "/api/weather");
        Assert.Contains(index.Endpoints, e => e.Route == "/api/weather/forecast");
        Assert.Contains(index.Endpoints, e => e.Route == "/api/items");
        Assert.Contains(index.Endpoints, e => e.Route == "/api/ping");
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

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/agents");

        Assert.NotNull(index);
        Assert.Empty(index.Groups);
    }
}
