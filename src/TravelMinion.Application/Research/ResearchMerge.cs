using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// The selection of a Research Job's output to persist: the candidates to write
/// back plus how many of the Trip's existing Suggestions they duplicate.
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
/// Merges a Research Job's output into a Trip's Suggestions. With
/// <see cref="ResearchMergeMode.Append"/> the list grows and the Trip's existing
/// Suggestions are returned unchanged (already-discarded candidates are excluded
/// by the Trip), so approvals and discards are never lost.
/// </summary>
public static class ResearchMerge
{
    public static ResearchMergeResult Combine(
        Trip trip,
        IReadOnlyList<Suggestion> researched,
        ResearchMergeMode mode)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(researched);

        if (mode == ResearchMergeMode.Replace)
        {
            return new ResearchMergeResult(ResearchMergeMode.Replace, researched, researched.Count, 0);
        }

        var existing = new HashSet<(string Name, string Destination)>(
            trip.Suggestions.Select(s => (s.Name.ToLowerInvariant(), s.Destination.ToLowerInvariant())));

        var appended = new List<Suggestion>();
        var duplicateCount = 0;
        var seen = new HashSet<(string Name, string Destination)>(existing);
        foreach (var suggestion in researched)
        {
            if (suggestion is null)
            {
                continue;
            }

            var key = (suggestion.Name.ToLowerInvariant(), suggestion.Destination.ToLowerInvariant());
            if (!seen.Add(key))
            {
                duplicateCount++;
                continue;
            }

            appended.Add(suggestion);
        }

        var merged = new List<Suggestion>(trip.Suggestions);
        merged.AddRange(appended);

        return new ResearchMergeResult(ResearchMergeMode.Append, merged, appended.Count, duplicateCount);
    }
}
