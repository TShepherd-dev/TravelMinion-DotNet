namespace TravelMinion.Domain;

/// <summary>
/// An itinerary day with time-blocked activities.
/// </summary>
public sealed class ActivityDay : ItineraryDay
{
    public ActivityDay(DateOnly date, string destination, IEnumerable<TimeBlock>? timeBlocks = null)
        : base(date, destination)
        => TimeBlocks = timeBlocks?.ToList() ?? new List<TimeBlock>();

    public override ItineraryDayType DayType => ItineraryDayType.Activity;

    public IReadOnlyList<TimeBlock> TimeBlocks { get; }
}
