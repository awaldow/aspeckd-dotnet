using System.Text.Json;
using Aspeckd.Models;
using Aspeckd.Services;
using Aspeckd.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Aspeckd.Tests;

/// <summary>
/// Compares the runtime-generated agent spec against golden-copy JSON files stored in the
/// <c>GoldenSpec/</c> directory (relative to the test binary output).
/// </summary>
/// <remarks>
/// <para>
/// The golden-copy files act as a regression guard: if any spec output changes unexpectedly
/// these tests will fail, forcing the developer to consciously update the golden copies
/// by setting the <c>ASPECKD_UPDATE_GOLDEN=1</c> environment variable and re-running the
/// tests.
/// </para>
/// <para>
/// To regenerate the golden copies run:
/// <code>
///   ASPECKD_UPDATE_GOLDEN=1 dotnet test --filter "FullyQualifiedName~GoldenSpec"
/// </code>
/// </para>
/// </remarks>
public class GoldenSpecTests : IClassFixture<TestWebAppFactory>
{
    private static readonly string GoldenDir =
        Path.Combine(AppContext.BaseDirectory, "GoldenSpec");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly bool UpdateMode =
        string.Equals(
            Environment.GetEnvironmentVariable("ASPECKD_UPDATE_GOLDEN"),
            "1",
            StringComparison.Ordinal);

    private readonly IAgentSpecProvider _provider;

    public GoldenSpecTests(TestWebAppFactory factory)
    {
        _provider = factory.Services.GetRequiredService<IAgentSpecProvider>();
    }

    // -----------------------------------------------------------------------
    // index.json
    // -----------------------------------------------------------------------

    [Fact]
    public void IndexMatchesGoldenCopy()
    {
        var actual = Serialize(_provider.GetIndex());
        CompareOrUpdate("index.json", actual);
    }

    // -----------------------------------------------------------------------
    // schemas.json
    // -----------------------------------------------------------------------

    [Fact]
    public void SchemasMatchGoldenCopy()
    {
        var actual = Serialize(_provider.GetSchemas());
        CompareOrUpdate("schemas.json", actual);
    }

    // -----------------------------------------------------------------------
    // Per-endpoint detail files
    // -----------------------------------------------------------------------

    [Fact]
    public void EndpointDetailsMatchGoldenCopies()
    {
        var index = _provider.GetIndex();

        foreach (var endpoint in index.Endpoints)
        {
            var detail = _provider.GetEndpointDetail(endpoint.Id);
            Assert.NotNull(detail);
            var actual = Serialize(detail);
            CompareOrUpdate($"{endpoint.Id}.json", actual);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, SerializerOptions);

    private static void CompareOrUpdate(string fileName, string actualJson)
    {
        var path = Path.Combine(GoldenDir, fileName);

        if (UpdateMode)
        {
            Directory.CreateDirectory(GoldenDir);
            File.WriteAllText(path, actualJson + Environment.NewLine);
            return;
        }

        Assert.True(
            File.Exists(path),
            $"Golden copy '{fileName}' not found in '{GoldenDir}'. " +
            "Run with ASPECKD_UPDATE_GOLDEN=1 to generate it.");

        var expected = File.ReadAllText(path).TrimEnd();
        Assert.Equal(expected, actualJson.TrimEnd());
    }
}
