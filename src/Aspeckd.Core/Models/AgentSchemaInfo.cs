namespace Aspeckd.Models;

/// <summary>
/// A named JSON schema surfaced at <c>/agents/schemas</c>.
/// </summary>
public sealed class AgentSchemaInfo
{
    /// <summary>Schema name (typically the CLR type name).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The JSON Schema as a raw JSON string. Populated from the OpenAPI document's
    /// component schemas when available.
    /// </summary>
    public string? JsonSchema { get; init; }
}
