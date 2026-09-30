using Microsoft.EntityFrameworkCore.ChangeTracking;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence;

/// <summary>
/// Value comparer so EF detects changes to a Research Job's JSON-serialised
/// progress list (a mutable in-memory list would otherwise only compare by
/// reference and changes would be missed).
/// </summary>
internal sealed class ResearchJobProgressValueComparer : ValueComparer<IReadOnlyList<ResearchJobProgress>>
{
    public ResearchJobProgressValueComparer()
        : base(
            (left, right) => left!.SequenceEqual(right!, ProgressComparer.Instance),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, ProgressComparer.Instance.GetHashCode(item))),
            value => value.ToList())
    {
    }

    private sealed class ProgressComparer : IEqualityComparer<ResearchJobProgress>
    {
        public static readonly ProgressComparer Instance = new();

        public bool Equals(ResearchJobProgress? x, ResearchJobProgress? y)
            => ReferenceEquals(x, y)
                || (x is not null && y is not null
                    && x.Destination == y.Destination
                    && x.SuggestionsFound == y.SuggestionsFound
                    && x.Completed == y.Completed);

        public int GetHashCode(ResearchJobProgress obj)
            => HashCode.Combine(obj.Destination, obj.SuggestionsFound, obj.Completed);
    }
}
