using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Runs the Research Step for a Trip and persists the resulting Suggestions.
/// </summary>
public interface IResearchRunner
{
    /// <summary>
    /// False when no LLM profile is configured, so the Research Step cannot run.
    /// </summary>
    bool IsAvailable { get; }

    Task<ResearchRunResult> RunAsync(Trip trip, CancellationToken cancellationToken = default);
}
