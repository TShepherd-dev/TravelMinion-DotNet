namespace TravelMinion.Domain;

/// <summary>
/// A city or town where the traveller is based within a <see cref="Country"/>.
/// Accommodation is implicit. Its position is its order within its country's
/// chain; it carries a day count and an optional transit from the previous base.
/// </summary>
public sealed class Base
{
    public Base(
        string name,
        int days,
        string? transitFromPrevious = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A base must have a name.");
        }

        if (days < 1)
        {
            throw new DomainException("A base must have at least one day.");
        }

        Name = name.Trim();
        Days = days;
        TransitFromPrevious = string.IsNullOrWhiteSpace(transitFromPrevious)
            ? null
            : transitFromPrevious.Trim();
    }

    /// <summary>The base name (city/town). Unique within a Trip.</summary>
    public string Name { get; }

    /// <summary>
    /// The number of days based here. Redistributed by <see cref="Country"/>
    /// so that a country's bases sum to its span.
    /// </summary>
    public int Days { get; internal set; }

    /// <summary>Rough transit from the previous base (e.g. "flight 3h").</summary>
    public string? TransitFromPrevious { get; }
}
