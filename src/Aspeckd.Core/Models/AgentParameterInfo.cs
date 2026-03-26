namespace Aspeckd.Models;

/// <summary>
/// Describes a single parameter on an API endpoint.
/// </summary>
public sealed class AgentParameterInfo
{
    /// <summary>Parameter name as it appears in the route/query string/header.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Source of the parameter: <c>Route</c>, <c>Query</c>, <c>Header</c>, or <c>Body</c>.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>CLR type name of the parameter (e.g. <c>String</c>, <c>Int32</c>).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Whether the parameter is required.</summary>
    public bool IsRequired { get; init; }
}
