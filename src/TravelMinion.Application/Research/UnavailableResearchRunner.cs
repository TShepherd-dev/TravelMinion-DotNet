using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Placeholder <see cref="IResearchRunner"/> used when no LLM profile is configured.
/// </summary>
public sealed class UnavailableResearchRunner : IResearchRunner
{
    public bool IsAvailable => false;

    public Task<ResearchRunResult> RunAsync(Trip trip, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "No LLM profile is configured. Set Llm:ApiKey so the Research Step can run.");
}
