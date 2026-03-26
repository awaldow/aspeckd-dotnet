using Aspeckd.Extensions;

// ── BasicSample ──────────────────────────────────────────────────────────────
// The simplest possible Aspeckd setup.
// Run the app and navigate to /agents to see the generated spec index.
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Required: registers IApiDescriptionGroupCollectionProvider, which Aspeckd
// uses to discover the app's endpoints.
builder.Services.AddEndpointsApiExplorer();

// 1. Register Aspeckd with a title and description.
builder.Services.AddAgentSpec(opt =>
{
    opt.Title       = "Basic Sample API";
    opt.Description = "A minimal API demonstrating how to add Aspeckd to an ASP.NET Core app.";
});

var app = builder.Build();

// 2. Register a few ordinary minimal-API endpoints.
app.MapGet("/hello",   () => Results.Ok(new { message = "Hello, world!" }));
app.MapGet("/goodbye", () => Results.Ok(new { message = "Goodbye, world!" }));

app.MapPost("/echo", (EchoRequest req) =>
    Results.Ok(new { echoed = req.Text }));

// 3. Map the three Aspeckd agent spec routes (/agents, /agents/schemas, /agents/{id}).
app.MapAgentSpec();

app.Run();

/// <summary>Request body used by the /echo endpoint.</summary>
record EchoRequest(string Text);
