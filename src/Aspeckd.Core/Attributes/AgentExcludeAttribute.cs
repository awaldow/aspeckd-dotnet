namespace Aspeckd.Attributes;

/// <summary>
/// Companion attribute that excludes an endpoint from the agent spec index.
/// Apply to an action method or controller class to suppress it from agent discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AgentExcludeAttribute : Attribute
{
}
