using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TravelMinion.Application;
using TravelMinion.Domain;
using TravelMinion.Infrastructure;
using TravelMinion.Infrastructure.Persistence;
using TravelMinion.Api.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddTravelMinionResearch(builder.Configuration);
builder.Services.AddTravelMinionPersistence(builder.Configuration);

var llmProfile = LlmProfileFactory.FromConfiguration(builder.Configuration);
if (llmProfile is not null)
{
    builder.Services.AddTravelMinionLlm(llmProfile);
}

builder.Services.AddScoped<ResearchProgressReporter>();
builder.Services.AddScoped(sp => new ResearchService(
    sp.GetRequiredService<ResearchEngine>(),
    progress: sp.GetRequiredService<ResearchProgressReporter>()));
builder.Services.AddScoped<TripService>();

if (llmProfile is not null)
{
    builder.Services.AddScoped<IResearchRunner, ResearchRunner>();
}
else
{
    builder.Services.AddScoped<IResearchRunner, UnavailableResearchRunner>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<TravelMinionDbContext>();
    if (database.Database.IsSqlite())
    {
        database.Database.EnsureCreated();
    }
    else
    {
        database.Database.Migrate();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { name = "TravelMinion", status = "ok" }))
    .WithName("GetStatus");

app.MapGet("/llm-profiles", () =>
{
    if (llmProfile is null)
    {
        return Results.Ok(Array.Empty<object>());
    }

    var profile = llmProfile;
    return Results.Ok(new[]
    {
        new { profile.Name, profile.Provider, profile.ModelId },
    });
})
.WithName("ListLlmProfiles");

if (app.Environment.IsDevelopment())
{
    if (llmProfile is not null)
    {
        // Development-only: runs the Research Step synchronously against the live
        // Tavily + LLM providers so the integration can be smoked end to end.
        app.MapPost("/dev/research-smoke", async (
            ResearchSmokeRequest request,
            ResearchService research,
            CancellationToken cancellationToken) =>
        {
            var brief = TripBrief.Create(
                new[] { new DestinationStop(request.Destination, request.Days) },
                request.StartDate,
                request.StartDate.AddDays(request.Days - 1),
                interests: request.Interests);

            var job = ResearchJob.Queue(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var result = await research.RunAsync(job, brief, cancellationToken);

            return Results.Ok(result.Suggestions);
        })
        .WithName("ResearchSmoke");
    }
    else
    {
        app.MapPost("/dev/research-smoke", () => Results.Problem(
            detail: "No LLM profile is configured. Set Llm:ApiKey (user-secrets) to enable research.",
            statusCode: StatusCodes.Status503ServiceUnavailable))
        .WithName("ResearchSmokeUnavailable");
    }
}

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>Request body for the development-only research smoke endpoint.</summary>
public sealed record ResearchSmokeRequest(
    string Destination,
    int Days,
    DateOnly StartDate,
    IReadOnlyList<string>? Interests = null);
