using Aspeckd.Configuration;

namespace Aspeckd.Tests;

public class AspeckdOptionsTests
{
    [Fact]
    public void DefaultBasePath_IsWellKnownAgents()
    {
        var options = new AspeckdOptions();
        Assert.Equal("/.well-known/agents", options.BasePath);
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

    [Fact]
    public void DefaultAuth_IsNull()
    {
        var options = new AspeckdOptions();
        Assert.Null(options.Auth);
    }

    [Fact]
    public void Auth_CanBeSet()
    {
        var options = new AspeckdOptions
        {
            Auth = new Aspeckd.Models.AgentAuthInfo
            {
                Scheme = "bearer",
                HeaderName = "Authorization"
            }
        };

        Assert.NotNull(options.Auth);
        Assert.Equal("bearer", options.Auth.Scheme);
        Assert.Equal("Authorization", options.Auth.HeaderName);
    }

    [Fact]
    public void DefaultDescriptionWarnings_IsTrue()
    {
        var options = new AspeckdOptions();
        Assert.True(options.DescriptionWarnings);
    }

    [Fact]
    public void DescriptionWarnings_CanBeDisabled()
    {
        var options = new AspeckdOptions { DescriptionWarnings = false };
        Assert.False(options.DescriptionWarnings);
    }

    [Fact]
    public void DefaultMinimumDescriptionLength_Is30()
    {
        var options = new AspeckdOptions();
        Assert.Equal(30, options.MinimumDescriptionLength);
    }

    [Fact]
    public void MinimumDescriptionLength_CanBeOverridden()
    {
        var options = new AspeckdOptions { MinimumDescriptionLength = 50 };
        Assert.Equal(50, options.MinimumDescriptionLength);
    }
}
