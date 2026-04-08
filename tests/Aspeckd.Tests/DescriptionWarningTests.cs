using Aspeckd.Services;
using Aspeckd.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspeckd.Tests;

/// <summary>
/// Verifies build-time description-quality warnings emitted by <see cref="IAgentSpecProvider"/>
/// for terse or missing descriptions on endpoints, groups, and the root API description.
/// </summary>
public class DescriptionWarningTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (IAgentSpecProvider Provider, IList<(LogLevel Level, string Message)> Logs)
        BuildProvider(Action<Aspeckd.Configuration.AspeckdOptions>? configure = null)
    {
        var logs = new List<(LogLevel Level, string Message)>();
        var factory = new DescriptionWarningTestWebAppFactory(logs, configure);
        var provider = factory.Services.GetRequiredService<IAgentSpecProvider>();
        return (provider, logs);
    }

    // -----------------------------------------------------------------------
    // Warnings enabled (default)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_EmitsAspeckd001_WhenEndpointDescriptionIsTerse()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // "Gets user" is 9 chars — well below the 30-char threshold.
        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD001") &&
            l.Message.Contains("get-api-user") &&
            l.Message.Contains("Gets user"));
    }

    [Fact]
    public void GetIndex_EmitsAspeckd002_WhenEndpointDescriptionIsNull()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // /api/order has no AgentDescriptionAttribute.
        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD002") &&
            l.Message.Contains("get-api-order"));
    }

    [Fact]
    public void GetIndex_DoesNotEmitWarning_WhenEndpointDescriptionIsLongEnough()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        Assert.DoesNotContain(logs, l => l.Message.Contains("get-api-products"));
    }

    [Fact]
    public void GetIndex_DoesNotEmitAspeckd001_WhenSuppressedOnEndpoint()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // /api/health has [AspeckdSuppressWarning("ASPECKD001")] — terse description suppressed.
        Assert.DoesNotContain(logs, l =>
            l.Message.Contains("ASPECKD001") && l.Message.Contains("get-api-health"));
    }

    [Fact]
    public void GetIndex_DoesNotEmitAspeckd002_WhenAllWarningsSuppressedOnEndpoint()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // /api/ping has [AspeckdSuppressWarning] (no codes = suppress all).
        Assert.DoesNotContain(logs, l => l.Message.Contains("get-api-ping"));
    }

    // -----------------------------------------------------------------------
    // Group description warnings
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_EmitsAspeckd001_WhenGroupDescriptionIsTerse()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // "Widgets" group has Description = "Widgets" (7 chars).
        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD001") &&
            l.Message.Contains("Widgets"));
    }

    [Fact]
    public void GetIndex_EmitsAspeckd002_WhenGroupDescriptionIsNull()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // "Gadgets" group has no Description.
        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD002") &&
            l.Message.Contains("Gadgets"));
    }

    [Fact]
    public void GetIndex_DoesNotEmitWarning_WhenGroupDescriptionSuppressed()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // "Tools" group representative endpoint has [AspeckdSuppressWarning("ASPECKD001")].
        Assert.DoesNotContain(logs, l =>
            l.Message.Contains("ASPECKD001") && l.Message.Contains("Tools"));
    }

    // -----------------------------------------------------------------------
    // Root description warnings
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_EmitsAspeckd001_WhenRootDescriptionIsTerse()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        // Root description is "Terse" (5 chars).
        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD001") &&
            l.Message.Contains("Terse"));
    }

    [Fact]
    public void GetIndex_EmitsAspeckd002_WhenRootDescriptionIsNull()
    {
        var (provider, logs) = BuildProvider(opt => opt.Description = null);
        provider.GetIndex();

        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD002") &&
            l.Message.Contains("root"));
    }

    // -----------------------------------------------------------------------
    // Global toggle
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_EmitsNoWarnings_WhenDescriptionWarningsFlagIsFalse()
    {
        var (provider, logs) = BuildProvider(opt => opt.DescriptionWarnings = false);
        provider.GetIndex();

        Assert.DoesNotContain(logs, l =>
            l.Level == LogLevel.Warning &&
            (l.Message.Contains("ASPECKD001") || l.Message.Contains("ASPECKD002")));
    }

    // -----------------------------------------------------------------------
    // Configurable threshold
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_DoesNotEmitAspeckd001_WhenThresholdIsLoweredBelowDescriptionLength()
    {
        // Lower threshold to 5 so "Gets user" (9 chars) no longer triggers a warning.
        var (provider, logs) = BuildProvider(opt => opt.MinimumDescriptionLength = 5);
        provider.GetIndex();

        Assert.DoesNotContain(logs, l =>
            l.Message.Contains("ASPECKD001") && l.Message.Contains("Gets user"));
    }

    [Fact]
    public void GetIndex_EmitsAspeckd001_WhenThresholdIsRaisedAboveDescriptionLength()
    {
        // Raise threshold to 100 so even "Returns the full catalogue..." triggers a warning.
        var (provider, logs) = BuildProvider(opt => opt.MinimumDescriptionLength = 100);
        provider.GetIndex();

        Assert.Contains(logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ASPECKD001") &&
            l.Message.Contains("get-api-products"));
    }

    // -----------------------------------------------------------------------
    // Idempotency — warnings emitted only once
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_EmitsWarningsOnlyOnce_WhenCalledMultipleTimes()
    {
        var (provider, logs) = BuildProvider();

        provider.GetIndex();
        provider.GetIndex();
        provider.GetIndex();

        // Each warning should appear exactly once.
        var aspeckd001ForUser = logs.Count(l =>
            l.Message.Contains("ASPECKD001") && l.Message.Contains("get-api-user"));

        Assert.Equal(1, aspeckd001ForUser);
    }

    // -----------------------------------------------------------------------
    // Warning message content
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIndex_Aspeckd001Message_ContainsSuppressionInstructions()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        var warning = logs.First(l => l.Message.Contains("ASPECKD001") && l.Message.Contains("get-api-user"));
        Assert.Contains("options.DescriptionWarnings = false", warning.Message);
        Assert.Contains("AspeckdSuppressWarning", warning.Message);
    }

    [Fact]
    public void GetIndex_Aspeckd002Message_ContainsSuppressionInstructions()
    {
        var (provider, logs) = BuildProvider();
        provider.GetIndex();

        var warning = logs.First(l => l.Message.Contains("ASPECKD002") && l.Message.Contains("get-api-order"));
        Assert.Contains("options.DescriptionWarnings = false", warning.Message);
        Assert.Contains("AspeckdSuppressWarning", warning.Message);
    }
}
