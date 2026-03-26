# Aspeckd Samples

This directory contains runnable sample projects that demonstrate the different use cases for [Aspeckd](../README.md).

---

## Projects

| Project | What it shows |
|---|---|
| [BasicSample](BasicSample/) | The minimal Aspeckd setup — `AddAgentSpec()` + `MapAgentSpec()` with title and description. |
| [AnnotatedSample](AnnotatedSample/) | A Todo API demonstrating all Aspeckd attributes (`[AgentName]`, `[AgentDescription]`, `[AgentExclude]`) and the `UseOpenApiMetadataFallback` option. |

---

## Running a sample

From the repo root, run any sample with `dotnet run`:

```bash
dotnet run --project samples/BasicSample
# or
dotnet run --project samples/AnnotatedSample
```

Then explore the agent spec endpoints in your browser or with `curl`:

| Endpoint | Description |
|---|---|
| `GET /agents` | Spec index — title, description, and a summary of every non-excluded endpoint. |
| `GET /agents/schemas` | All named request/response schemas. |
| `GET /agents/{id}` | Full detail for a single endpoint. |

### Example (BasicSample)

```bash
curl http://localhost:5000/agents
```

```json
{
  "title": "Basic Sample API",
  "description": "A minimal API demonstrating how to add Aspeckd to an ASP.NET Core app.",
  "schemasUrl": "/agents/schemas",
  "endpoints": [
    {
      "id": "get-hello",
      "name": "GET /hello",
      "httpMethod": "GET",
      "route": "/hello",
      "description": null,
      "detailUrl": "/agents/get-hello"
    },
    ...
  ]
}
```

---

## Feature coverage

| Feature | BasicSample | AnnotatedSample |
|---|:---:|:---:|
| `AddAgentSpec()` + `MapAgentSpec()` | ✅ | ✅ |
| `opt.Title` / `opt.Description` | ✅ | ✅ |
| `[AgentName]` attribute | | ✅ |
| `[AgentDescription]` attribute | | ✅ |
| `[AgentExclude]` attribute | | ✅ |
| `opt.UseOpenApiMetadataFallback` | | ✅ |
