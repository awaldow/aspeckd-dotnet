# Aspeckd.Core

Core abstractions for **aspeckd** — the .NET library that surfaces agent-readable spec endpoints alongside your ASP.NET Core API.

[![NuGet](https://img.shields.io/nuget/v/Aspeckd.Core)](https://www.nuget.org/packages/Aspeckd.Core)
[![CI](https://github.com/awaldow/aspeckd-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/awaldow/aspeckd-dotnet/actions/workflows/ci.yml)

> **Building an application, not a class library?** You most likely want [`Aspeckd`](https://www.nuget.org/packages/Aspeckd) instead — it includes this package and the ASP.NET Core integration in one step.

---

## When to use this package

Use `Aspeckd.Core` when you need the aspeckd attributes, models, or configuration types in a **class library** (or any project) that must **not** take a direct `Microsoft.AspNetCore.App` framework reference.

```bash
dotnet add package Aspeckd.Core
```

---

## What's included

### Companion attributes

Three attributes let you control how each endpoint appears in the agent spec:

#### `[AgentDescription]`

Provides an agent-focused description for an endpoint. Use it instead of (or alongside) `WithSummary()` / `WithDescription()`.

```csharp
[AgentDescription("Returns the current weather for the given city.")]
public IResult GetWeather(string city) { ... }
```

#### `[AgentName]`

Overrides the display name used for an endpoint in the spec index. When omitted, aspeckd auto-generates a name from the HTTP method and route template (e.g. `GET /api/weather/{city}`).

```csharp
[AgentName("GetWeather")]
[AgentDescription("Returns the current weather for the given city.")]
public IResult GetWeather(string city) { ... }
```

#### `[AgentExclude]`

Suppresses an endpoint from the agent spec entirely. Useful for internal or administrative endpoints that agents should not discover.

```csharp
[AgentExclude]
public IResult Ping() => Results.Ok();
```

---

### `AspeckdOptions`

Configuration type used when registering the `Aspeckd` integration package. Can be referenced from any project that depends on `Aspeckd.Core`.

| Property | Type | Default | Description |
|---|---|---|---|
| `BasePath` | `string` | `"/agents"` | The route prefix under which all agent spec endpoints are served. |
| `Title` | `string?` | `null` | Title shown in the agent spec index. |
| `Description` | `string?` | `null` | Optional description shown at the top of the agent spec index. |
| `UseOpenApiMetadataFallback` | `bool` | `false` | When `true`, falls back to `WithSummary()` / `WithDescription()` / `WithName()` when aspeckd-specific attributes are absent. |

---

### `IAgentSpecProvider`

Interface for a custom agent spec provider. Implement this if you need full control over how the spec is built (e.g. to aggregate specs from multiple services).

---

## Links

- [GitHub repository](https://github.com/awaldow/aspeckd-dotnet)
- [Full documentation & contributing guide](https://github.com/awaldow/aspeckd-dotnet#readme)
- [`Aspeckd` package](https://www.nuget.org/packages/Aspeckd) — ASP.NET Core integration (most apps want this)
