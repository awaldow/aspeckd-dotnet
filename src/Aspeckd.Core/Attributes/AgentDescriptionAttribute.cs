namespace Aspeckd.Attributes;

/// <summary>
/// Companion attribute to standard OpenAPI attributes that provides an agent-focused description
/// for an API endpoint. This description is surfaced in the agent spec index at the configured
/// base path (default: <c>/agents</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AgentDescriptionAttribute : Attribute
{
    /// <summary>
    /// Gets the agent-focused description of the endpoint.
    /// </summary>
    public string Description { get; }

    /// <param name="description">A concise, agent-readable description of what the endpoint does.</param>
    public AgentDescriptionAttribute(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = description;
    }
}
