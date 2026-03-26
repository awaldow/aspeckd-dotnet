using Aspeckd.Attributes;
using Aspeckd.Extensions;

// ── AnnotatedSample ───────────────────────────────────────────────────────────
// A Todo API that demonstrates all Aspeckd annotations and configuration options:
//
//   • [AgentName]        – override the display name shown in the spec index
//   • [AgentDescription] – provide an agent-optimised endpoint description
//   • [AgentExclude]     – hide internal endpoints from the agent spec entirely
//   • UseOpenApiMetadataFallback – reuse WithName() / WithSummary() metadata
//     so you don't have to duplicate it with Aspeckd attributes
//
// Run the app and visit:
//   GET /agents           – spec index
//   GET /agents/schemas   – schema list
//   GET /agents/{id}      – detail for a single endpoint
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Required: registers IApiDescriptionGroupCollectionProvider, which Aspeckd
// uses to discover the app's endpoints.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAgentSpec(opt =>
{
    opt.Title       = "Todo API";
    opt.Description = "A simple Todo API that demonstrates Aspeckd annotations.";

    // When true, endpoints that do not have [AgentName] / [AgentDescription]
    // fall back to the standard OpenAPI metadata (WithName / WithSummary /
    // WithDescription).  See the /todos/count endpoint below for an example.
    opt.UseOpenApiMetadataFallback = true;
});

var app = builder.Build();

// ── In-memory data store ──────────────────────────────────────────────────────
var todos = new List<TodoItem>
{
    new(1, "Buy groceries",  IsComplete: false),
    new(2, "Walk the dog",   IsComplete: true),
    new(3, "Read a chapter", IsComplete: false),
};
var nextId = todos.Count + 1;

// ── Endpoints ─────────────────────────────────────────────────────────────────

// [AgentName] + [AgentDescription]: explicit Aspeckd annotations take highest priority.
app.MapGet("/todos",
    [AgentName("ListTodos")]
    [AgentDescription("Returns all todo items. Pass ?completed=true/false to filter by status.")]
    (bool? completed) =>
    {
        var result = completed.HasValue
            ? todos.Where(t => t.IsComplete == completed.Value)
            : todos.AsEnumerable();

        return Results.Ok(result);
    });

app.MapGet("/todos/{id:int}",
    [AgentName("GetTodo")]
    [AgentDescription("Returns the todo item with the given ID, or 404 if it does not exist.")]
    (int id) =>
        todos.FirstOrDefault(t => t.Id == id) is { } todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todos",
    [AgentName("CreateTodo")]
    [AgentDescription("Creates a new todo item. The new item is returned with its assigned ID.")]
    (CreateTodoRequest req) =>
    {
        var todo = new TodoItem(nextId++, req.Title, IsComplete: false);
        todos.Add(todo);
        return Results.Created($"/todos/{todo.Id}", todo);
    });

app.MapPut("/todos/{id:int}",
    [AgentName("UpdateTodo")]
    [AgentDescription("Replaces the title and completion status of an existing todo item.")]
    (int id, UpdateTodoRequest req) =>
    {
        var idx = todos.FindIndex(t => t.Id == id);
        if (idx == -1) return Results.NotFound();
        todos[idx] = todos[idx] with { Title = req.Title, IsComplete = req.IsComplete };
        return Results.Ok(todos[idx]);
    });

app.MapDelete("/todos/{id:int}",
    [AgentName("DeleteTodo")]
    [AgentDescription("Permanently removes the todo item with the given ID.")]
    (int id) =>
    {
        var todo = todos.FirstOrDefault(t => t.Id == id);
        if (todo is null) return Results.NotFound();
        todos.Remove(todo);
        return Results.NoContent();
    });

// UseOpenApiMetadataFallback: no Aspeckd attributes here — Aspeckd will fall
// back to the name and summary supplied via WithName() / WithSummary().
app.MapGet("/todos/count", () => Results.Ok(new { total = todos.Count, completed = todos.Count(t => t.IsComplete) }))
    .WithName("CountTodos")
    .WithSummary("Returns the total number of todos and how many are completed.");

// [AgentExclude]: this endpoint is for internal use only and will not appear
// in the agent spec at all.
app.MapGet("/todos/internal/stats",
    [AgentExclude]
    () => new
    {
        Total     = todos.Count,
        Completed = todos.Count(t => t.IsComplete),
        Pending   = todos.Count(t => !t.IsComplete),
    });

// Map the three Aspeckd agent spec routes (/agents, /agents/schemas, /agents/{id}).
app.MapAgentSpec();

app.Run();

// ── Models ────────────────────────────────────────────────────────────────────
record TodoItem(int Id, string Title, bool IsComplete);
record CreateTodoRequest(string Title);
record UpdateTodoRequest(string Title, bool IsComplete);
