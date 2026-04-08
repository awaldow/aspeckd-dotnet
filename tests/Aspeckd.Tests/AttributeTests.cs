using Aspeckd.Attributes;

namespace Aspeckd.Tests;

public class AttributeTests
{
    [Fact]
    public void AgentDescriptionAttribute_StoresDescription()
    {
        var attr = new AgentDescriptionAttribute("Fetches weather data");
        Assert.Equal("Fetches weather data", attr.Description);
    }

    [Fact]
    public void AgentDescriptionAttribute_ThrowsOnNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new AgentDescriptionAttribute(""));
        Assert.Throws<ArgumentException>(() => new AgentDescriptionAttribute("   "));
        Assert.Throws<ArgumentNullException>(() => new AgentDescriptionAttribute(null!));
    }

    [Fact]
    public void AgentNameAttribute_StoresName()
    {
        var attr = new AgentNameAttribute("Get Weather");
        Assert.Equal("Get Weather", attr.Name);
    }

    [Fact]
    public void AgentNameAttribute_ThrowsOnNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new AgentNameAttribute(""));
        Assert.Throws<ArgumentException>(() => new AgentNameAttribute("   "));
        Assert.Throws<ArgumentNullException>(() => new AgentNameAttribute(null!));
    }

    [Fact]
    public void AgentExcludeAttribute_CanBeInstantiated()
    {
        var attr = new AgentExcludeAttribute();
        Assert.NotNull(attr);
    }

    [Fact]
    public void AgentDescriptionAttribute_IsInherited()
    {
        var usage = typeof(AgentDescriptionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.True(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }

    [Fact]
    public void AgentNameAttribute_IsInherited()
    {
        var usage = typeof(AgentNameAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.True(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }

    [Fact]
    public void AgentExcludeAttribute_IsInherited()
    {
        var usage = typeof(AgentExcludeAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.True(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }

    [Fact]
    public void AgentToolGroupAttribute_StoresName()
    {
        var attr = new AgentToolGroupAttribute("Weather");
        Assert.Equal("Weather", attr.Name);
    }

    [Fact]
    public void AgentToolGroupAttribute_ThrowsOnNullOrWhitespaceName()
    {
        Assert.Throws<ArgumentException>(() => new AgentToolGroupAttribute(""));
        Assert.Throws<ArgumentException>(() => new AgentToolGroupAttribute("   "));
        Assert.Throws<ArgumentNullException>(() => new AgentToolGroupAttribute(null!));
    }

    [Fact]
    public void AgentToolGroupAttribute_DescriptionDefaultsToNull()
    {
        var attr = new AgentToolGroupAttribute("Weather");
        Assert.Null(attr.Description);
    }

    [Fact]
    public void AgentToolGroupAttribute_StoresDescription()
    {
        var attr = new AgentToolGroupAttribute("Weather") { Description = "Weather-related endpoints" };
        Assert.Equal("Weather-related endpoints", attr.Description);
    }

    [Fact]
    public void AgentToolGroupAttribute_RequiredClaimsDefaultsToEmpty()
    {
        var attr = new AgentToolGroupAttribute("Weather");
        Assert.Empty(attr.RequiredClaims);
    }

    [Fact]
    public void AgentToolGroupAttribute_StoresRequiredClaims()
    {
        var attr = new AgentToolGroupAttribute("Weather")
        {
            RequiredClaims = new string[] { "weather:read", "weather:forecast" }
        };
        Assert.Equal(new string[] { "weather:read", "weather:forecast" }, attr.RequiredClaims);
    }

    [Fact]
    public void AgentToolGroupAttribute_IsInherited()
    {
        var usage = typeof(AgentToolGroupAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.True(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }

    [Fact]
    public void AgentRequiredClaimsAttribute_StoresClaims()
    {
        var attr = new AgentRequiredClaimsAttribute("orders:read", "orders:write");
        Assert.Equal(new[] { "orders:read", "orders:write" }, attr.Claims);
    }

    [Fact]
    public void AgentRequiredClaimsAttribute_ClaimsDefaultToEmpty()
    {
        var attr = new AgentRequiredClaimsAttribute();
        Assert.Empty(attr.Claims);
    }

    [Fact]
    public void AgentRequiredClaimsAttribute_IsInherited()
    {
        var usage = typeof(AgentRequiredClaimsAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.True(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }
}
