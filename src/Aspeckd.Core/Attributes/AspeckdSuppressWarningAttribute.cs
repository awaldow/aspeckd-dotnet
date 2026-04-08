namespace Aspeckd.Attributes;

/// <summary>
/// Suppresses one or more Aspeckd build-time description warnings for the annotated endpoint
/// or controller class.
/// </summary>
/// <remarks>
/// <para>
/// Apply with a specific warning code to suppress only that warning:
/// <code>
/// [AspeckdSuppressWarning("ASPECKD001")]
/// [HttpGet("health")]
/// public IActionResult HealthCheck() { ... }
/// </code>
/// </para>
/// <para>
/// Apply without arguments to suppress all Aspeckd description warnings for the endpoint:
/// <code>
/// [AspeckdSuppressWarning]
/// [HttpGet("health")]
/// public IActionResult HealthCheck() { ... }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AspeckdSuppressWarningAttribute : Attribute
{
    /// <summary>
    /// Gets the warning codes suppressed by this attribute.
    /// An empty array means all Aspeckd description warnings are suppressed.
    /// </summary>
    public string[] Codes { get; }

    /// <param name="codes">
    /// One or more warning codes to suppress (e.g. <c>"ASPECKD001"</c>, <c>"ASPECKD002"</c>).
    /// When omitted, all Aspeckd description warnings are suppressed for the target endpoint.
    /// </param>
    public AspeckdSuppressWarningAttribute(params string[] codes)
    {
        Codes = codes ?? [];
    }
}
