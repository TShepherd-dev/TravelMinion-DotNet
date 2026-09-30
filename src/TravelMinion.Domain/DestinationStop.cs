namespace TravelMinion.Domain;

/// <summary>
/// A single destination within a Trip, carrying its position in the itinerary
/// order, its day count, and an optional transit from the previous stop.
/// </summary>
public sealed class DestinationStop
{
    public DestinationStop(
        string destination,
        int days,
        int? order = null,
        string? transitFromPrevious = null)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new DomainException("A destination stop must name a destination.");
        }

        if (days < 1)
        {
            throw new DomainException("A destination stop must have at least one day.");
        }

        Destination = destination.Trim();
        Days = days;
        Order = order;
        TransitFromPrevious = string.IsNullOrWhiteSpace(transitFromPrevious)
            ? null
            : transitFromPrevious.Trim();
    }

    /// <summary>The destination name (city/country).</summary>
    public string Destination { get; }

    /// <summary>
    /// The number of days at this destination. Redistributed by
    /// <see cref="TripBrief.Create"/> so that stops sum to the trip span.
    /// </summary>
    public int Days { get; internal set; }

    /// <summary>Explicit 0-indexed order, or null when unset.</summary>
    public int? Order { get; }

    /// <summary>Rough transit from the previous stop (e.g. "flight 3h").</summary>
    public string? TransitFromPrevious { get; }
}
