namespace TravelMinion.Domain;

/// <summary>
/// An itinerary day with no planned activities: the "nothing" travel style, or
/// recovery after long travel.
/// </summary>
public sealed class FreeDay : ItineraryDay
{
    public FreeDay(DateOnly date, string destination, string? notes = null)
        : base(date, destination)
        => Notes = notes;

    public override ItineraryDayType DayType => ItineraryDayType.Free;

    public string? Notes { get; }
}
