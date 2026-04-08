using System.Text.Json.Serialization;

namespace Aspeckd.Models;

/// <summary>
/// Describes the authentication mechanism for an API or a group of endpoints.
/// This object can appear at the root level of the agent spec index (describing the API's
/// overall auth) or at the group level (overriding or extending the root-level auth for
/// a specific tool group).
/// </summary>
/// <remarks>
/// <para>
/// When present at the root level, agents use this to understand how to send credentials
/// and whether programmatic token acquisition is possible.  When present at the group
/// level, only the non-<see langword="null"/> fields override the root values; all others
/// are inherited.  Absent fields in the serialised JSON follow the same rule — absent means
/// inherit from root.
/// </para>
/// <para>
/// The <see cref="Instructions"/> field is intentionally free-text (markdown supported).
/// Its purpose is to give agents something useful to relay to the user when auth fails —
/// not to model every possible auth flow.
/// </para>
/// </remarks>
public sealed class AgentAuthInfo
{
    /// <summary>
    /// The authentication scheme type.
    /// Common values: <c>"bearer"</c>, <c>"apiKey"</c>, <c>"basic"</c>.
    /// Omitted from serialisation when <see langword="null"/> (inherit from root).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scheme { get; init; }

    /// <summary>
    /// The HTTP header name used to transmit the credential (e.g. <c>"Authorization"</c>
    /// for Bearer tokens, or a custom header for API keys).
    /// Omitted from serialisation when <see langword="null"/> (inherit from root).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HeaderName { get; init; }

    /// <summary>
    /// URL of the programmatic token endpoint, or <see langword="null"/> when no
    /// programmatic acquisition path exists.  A <see langword="null"/> value is an explicit
    /// signal — agents should not attempt programmatic token acquisition.
    /// Omitted from serialisation when <see langword="null"/> (inherit from root, or no
    /// programmatic path when at the root level).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TokenEndpoint { get; init; }

    /// <summary>
    /// OAuth2 grant type if applicable (e.g. <c>"client_credentials"</c>,
    /// <c>"authorization_code"</c>), or <see langword="null"/> when not applicable.
    /// Omitted from serialisation when <see langword="null"/> (inherit from root).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GrantType { get; init; }

    /// <summary>
    /// Human-readable instructions the agent can relay to the user when it cannot
    /// self-service authentication.  Markdown content is supported.  This is the
    /// fallback for any auth flow that requires human involvement.
    /// Omitted from serialisation when <see langword="null"/> (inherit from root).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; init; }
}
