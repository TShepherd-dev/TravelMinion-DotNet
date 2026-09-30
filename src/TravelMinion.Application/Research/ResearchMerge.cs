using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// The outcome of merging a Research Job's output into a Trip: the mode used, the
/// Trip's resulting Suggestions, and how many candidates were appended versus
/// recognised as duplicates.
/// </summary>
public sealed record ResearchMergeResult(
    ResearchMergeMode Mode,
    IReadOnlyList<Suggestion> Suggestions,
    int AppendedCount,
    int DuplicateCount);

/// <summary>How a Research Job's output is merged into a Trip's Suggestions.</summary>
public enum ResearchMergeMode
{
    /// <summary>The new job's output supersedes the prior set.</summary>
    Replace,

    /// <summary>New candidates are appended; existing ones are kept.</summary>
    Append,
}

/// <summary>
/// Applies a Research Job's output to a Trip's Suggestions. With
/// <see cref="ResearchMergeMode.Append"/> the list grows and the Trip's existing
/// Suggestions are kept - including discarded ones, which still count as present
/// so research never resurrects them - so approvals and discards are never lost.
/// De-duplication lives in <see cref="Trip.AppendSuggestions"/>.
/// </summary>
public static class ResearchMerge
{
    public static ResearchMergeResult Apply(
        Trip trip,
        IReadOnlyList<Suggestion> researched,
        ResearchMergeMode mode)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(researched);

        if (mode == ResearchMergeMode.Replace)
        {
            trip.ReplaceSuggestions(researched);
            return new ResearchMergeResult(ResearchMergeMode.Replace, trip.Suggestions, researched.Count, 0);
        }

        var appended = trip.AppendSuggestions(researched);
        var candidates = researched.Count(suggestion => suggestion is not null);

        return new ResearchMergeResult(
            ResearchMergeMode.Append,
            trip.Suggestions,
            appended.Count,
            candidates - appended.Count);
    }
}
