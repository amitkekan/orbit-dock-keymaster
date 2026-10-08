using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Orbit.LocalDemo;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("The synthetic local demo runs only in Development.");

builder.WebHost.ConfigureKestrel((context, options) =>
{
    // Ignore configured endpoints so configuration cannot expand the simulator's listener.
    options.Configure(new ConfigurationBuilder().Build());
    options.Listen(IPAddress.Loopback, 5081);
});
builder.Services.AddRazorPages();
builder.Services.AddSingleton<Simulation>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'none'; style-src 'self'; script-src 'none'; connect-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.CacheControl = "no-store";

    if (context.Connection.RemoteIpAddress is not { } address ||
        !IPAddress.IsLoopback(address) ||
        !string.Equals(context.Request.Host.Value, "127.0.0.1:5081", StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
    {
        context.Response.Headers.Allow = "GET, HEAD";
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        return;
    }

    await next(context);
});
app.UseStaticFiles();
app.MapRazorPages();
app.MapGet("/healthz", () => TypedResults.Ok(new { status = "healthy", mode = "synthetic-simulation" }));
app.MapGet("/simulation/{personaId}/fleets", (string personaId, Simulation simulation) =>
    TypedResults.Ok(simulation.GetFleets(personaId)));
app.MapGet("/simulation/{personaId}/fleets/{fleetId}/missions",
    Results<Ok<IReadOnlyList<MissionDecision>>, NotFound> (string personaId, string fleetId, Simulation simulation) =>
        simulation.GetMissions(personaId, fleetId) is { } missions
            ? TypedResults.Ok(missions) : TypedResults.NotFound());
app.MapGet("/simulation/{personaId}/fleets/{fleetId}/missions/{missionId}",
    Results<Ok<MissionDecision>, NotFound> (string personaId, string fleetId, string missionId, Simulation simulation) =>
        simulation.GetMission(personaId, fleetId, missionId) is { } mission
            ? TypedResults.Ok(mission) : TypedResults.NotFound());
app.Run();
