namespace Aspeckd.Attributes;

/// <summary>
/// Companion attribute that overrides the display name used for an endpoint in the agent spec index.
/// When omitted the endpoint's HTTP method + route template is used as the name.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AgentNameAttribute : Attribute
{
    /// <summary>
    /// Gets the human-readable name used to identify the endpoint in agent spec output.
    /// </summary>
    public string Name { get; }

    /// <param name="name">A short, human-readable name for the endpoint.</param>
    public AgentNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
