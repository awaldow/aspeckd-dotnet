namespace Aspeckd.Attributes;

/// <summary>
/// Declares the authorization claim types that must be present to call this endpoint.
/// The claims are surfaced in the agent spec detail document so that agents can
/// surface helpful context when authorization fails.
/// </summary>
/// <remarks>
/// When an endpoint belongs to a <see cref="AgentToolGroupAttribute"/> the group's
/// <see cref="AgentToolGroupAttribute.RequiredClaims"/> express claims shared by the
/// whole group.  Use <see cref="AgentRequiredClaimsAttribute"/> to declare claims that
/// are specific to a single endpoint, or to augment the group claims.
/// </remarks>
/// <example>
/// <code>
/// endpoints.MapGet(
///     "/api/orders/{id}",
///     [AgentRequiredClaims("orders:read")]
///     (string id) => Results.Ok(id));
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AgentRequiredClaimsAttribute : Attribute
{
    /// <summary>
    /// Gets the claim types required to call this endpoint.
    /// </summary>
    public IReadOnlyList<string> Claims { get; }

    /// <param name="claims">
    /// One or more claim type strings (e.g. <c>"orders:read"</c>, <c>"admin"</c>).
    /// </param>
    public AgentRequiredClaimsAttribute(params string[] claims)
    {
        Claims = claims;
    }
}
