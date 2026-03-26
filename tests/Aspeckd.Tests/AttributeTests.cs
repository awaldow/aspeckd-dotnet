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
}
