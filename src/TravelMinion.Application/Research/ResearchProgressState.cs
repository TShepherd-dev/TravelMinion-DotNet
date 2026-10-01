using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// A snapshot of a running Research Job's Research Progress: the stage it is in,
/// the destination being worked, and the Suggestions found so far. Transient -
/// it describes a Job in flight and is never persisted.
/// </summary>
public sealed record ResearchProgressState(
    ResearchStage Stage,
    string? Destination,
    int DestinationIndex,
    int DestinationCount,
    IReadOnlyList<Suggestion> Suggestions)
{
    /// <summary>The state before any run has started.</summary>
    public static ResearchProgressState Idle { get; } =
        new(ResearchStage.Idle, null, 0, 0, Array.Empty<Suggestion>());

    /// <summary>Whether a run is currently in flight.</summary>
    public bool IsRunning => Stage is ResearchStage.SearchingSources
        or ResearchStage.ReadingPages
        or ResearchStage.ExtractingActivities;
}
