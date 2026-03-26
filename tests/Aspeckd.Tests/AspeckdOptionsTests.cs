using Aspeckd.Configuration;

namespace Aspeckd.Tests;

public class AspeckdOptionsTests
{
    [Fact]
    public void DefaultBasePath_IsAgents()
    {
        var options = new AspeckdOptions();
        Assert.Equal("/agents", options.BasePath);
    }

    [Fact]
    public void DefaultTitle_IsNull()
    {
        var options = new AspeckdOptions();
        Assert.Null(options.Title);
    }

    [Fact]
    public void DefaultDescription_IsNull()
    {
        var options = new AspeckdOptions();
        Assert.Null(options.Description);
    }

    [Fact]
    public void BasePath_CanBeOverridden()
    {
        var options = new AspeckdOptions { BasePath = "/ai" };
        Assert.Equal("/ai", options.BasePath);
    }
}
