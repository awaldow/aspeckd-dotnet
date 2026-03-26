# aspeckd

A .NET library that surfaces **agent-readable spec endpoints** alongside your existing ASP.NET Core API.  
Instead of pointing an AI agent at a large OpenAPI document, aspeckd serves a small set of purpose-built JSON endpoints that describe your API in a compact, agent-friendly format — analogous to `llms.txt` for APIs.

[![NuGet](https://img.shields.io/nuget/v/Aspeckd)](https://www.nuget.org/packages/Aspeckd)
[![NuGet](https://img.shields.io/nuget/v/Aspeckd.Core?label=Aspeckd.Core)](https://www.nuget.org/packages/Aspeckd.Core)
[![CI](https://github.com/awaldow/aspeckd-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/awaldow/aspeckd-dotnet/actions/workflows/ci.yml)

---

## Packages

| Package | Purpose |
|---|---|
| **`Aspeckd`** | ASP.NET Core integration — registers DI services and maps the agent spec routes. Most apps only need this package. |
| **`Aspeckd.Core`** | Stable abstractions only — attributes, models, `AspeckdOptions`, and `IAgentSpecProvider`. No ASP.NET Core dependency; use this in class libraries that must not take a framework reference. |

---

## Installation

```bash
dotnet add package Aspeckd
```

If you only need the companion attributes and models (e.g. in a domain library), add `Aspeckd.Core` instead:

```bash
dotnet add package Aspeckd.Core
```

---

## Quick start

### 1 — Register services

Call `AddAgentSpec()` in your service-registration code (typically `Program.cs`):

```csharp
builder.Services.AddAgentSpec(opt =>
{
    opt.Title       = "My API";
    opt.Description = "What this API does, in one or two sentences.";
});
```

### 2 — Map the routes

After `app.Build()`, call `MapAgentSpec()` to activate the three agent spec endpoints:

```csharp
app.MapAgentSpec();
```

That's it. Three new routes are now live:

| Route | Description |
|---|---|
| `GET /agents` | Spec index — title, description, and a summary of every non-excluded endpoint |
| `GET /agents/schemas` | All named request/response schemas extracted from API metadata |
| `GET /agents/{id}` | Full detail for a single endpoint identified by `{id}` |

---

## Annotating endpoints

Aspeckd discovers endpoints through ASP.NET Core's built-in `IApiDescriptionGroupCollectionProvider`. Three companion attributes let you control how each endpoint appears in the agent spec.

### `[AgentDescription]`

Provides an agent-focused description for an endpoint. Use it instead of (or alongside) `WithSummary()` / `WithDescription()` to write a description optimised for agent consumption.

```csharp
app.MapGet("/api/weather/{city}", [AgentDescription("Returns the current weather for the given city.")] (string city) =>
    Results.Ok(WeatherService.Get(city)));
```

### `[AgentName]`

Overrides the display name used for an endpoint in the agent spec index. When omitted, aspeckd auto-generates a name from the HTTP method and route template (e.g. `GET /api/weather/{city}`).

```csharp
app.MapGet("/api/weather/{city}", [AgentName("GetWeather")] [AgentDescription("...")] (string city) =>
    Results.Ok(WeatherService.Get(city)));
```

### `[AgentExclude]`

Suppresses an endpoint from the agent spec entirely. Useful for internal or administrative endpoints that agents should not discover.

```csharp
app.MapGet("/internal/ping", [AgentExclude] () => Results.Ok());
```

---

## Configuration reference

All options are set through the `AspeckdOptions` delegate passed to `AddAgentSpec()`:

| Option | Type | Default | Description |
|---|---|---|---|
| `BasePath` | `string` | `"/agents"` | The route prefix under which all agent spec endpoints are served. |
| `Title` | `string?` | `null` (falls back to `"API"`) | Title shown in the agent spec index. |
| `Description` | `string?` | `null` | Optional description shown at the top of the agent spec index. |
| `UseOpenApiMetadataFallback` | `bool` | `false` | When `true`, endpoints without `[AgentDescription]` / `[AgentName]` fall back to the standard OpenAPI metadata set by `WithSummary()`, `WithDescription()`, and `WithName()`. |

### Custom base path

```csharp
builder.Services.AddAgentSpec(opt => opt.BasePath = "/ai-spec");
// Endpoints are now at /ai-spec, /ai-spec/schemas, /ai-spec/{id}
```

### Reusing OpenAPI metadata

If your endpoints already have `WithSummary()` / `WithDescription()` / `WithName()` annotations you can avoid duplicating them:

```csharp
builder.Services.AddAgentSpec(opt => opt.UseOpenApiMetadataFallback = true);
```

With fallback enabled the resolution priority for **name** is:

1. `[AgentName]` attribute
2. `WithName()` / `IEndpointNameMetadata`
3. Auto-generated `"METHOD /route"`

And for **description**:

1. `[AgentDescription]` attribute
2. `WithSummary()`
3. `WithDescription()`
4. `null`

---

## Example responses

### `GET /agents`

```json
{
  "title": "My API",
  "description": "What this API does, in one or two sentences.",
  "schemasUrl": "/agents/schemas",
  "endpoints": [
    {
      "id": "get-api-weather-city",
      "name": "GetWeather",
      "httpMethod": "GET",
      "route": "/api/weather/{city}",
      "description": "Returns the current weather for the given city.",
      "detailUrl": "/agents/get-api-weather-city"
    }
  ]
}
```

### `GET /agents/get-api-weather-city`

```json
{
  "id": "get-api-weather-city",
  "name": "GetWeather",
  "httpMethod": "GET",
  "route": "/api/weather/{city}",
  "description": "Returns the current weather for the given city.",
  "consumesContentTypes": [],
  "responseTypes": {
    "200": ["application/json"]
  },
  "parameters": [
    {
      "name": "city",
      "source": "Route",
      "type": "String",
      "isRequired": true
    }
  ]
}
```

### `GET /agents/schemas`

```json
[
  { "name": "WeatherForecast", "jsonSchema": null }
]
```

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for project structure, coding conventions, and development setup.
