namespace Aspeckd.Attributes;

/// <summary>
/// Groups an endpoint under a named tool group in the agent spec index.
/// Tool groups allow agents to discover related operations together and provide
/// shared context such as a description and required authorization claims.
/// </summary>
/// <remarks>
/// When applied to a class (e.g. an MVC controller) every action in that class
/// is placed in the group unless a method-level <see cref="AgentToolGroupAttribute"/>
/// overrides it.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AgentToolGroupAttribute : Attribute
{
    /// <summary>
    /// Gets the display name used to identify this tool group in agent spec output.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets an optional description that helps agents understand the purpose of this group.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the authorization claim types that are required to call operations in this group.
    /// Surfaced in the agent spec so that agents can better contextualize auth failures.
    /// </summary>
    public string[] RequiredClaims { get; init; } = [];

    /// <summary>
    /// Gets the human-readable authentication instructions specific to this group.
    /// When set, these instructions override the <c>instructions</c> field from the
    /// root-level auth block for endpoints in this group.  Useful when a subset of
    /// endpoints requires elevated access beyond the API baseline (for example, PIM
    /// activation or a specific role assignment).
    /// Markdown content is supported.
    /// </summary>
    public string? AuthInstructions { get; init; }

    /// <param name="name">A short, human-readable name for the group (e.g. <c>"Weather"</c>).</param>
    public AgentToolGroupAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
