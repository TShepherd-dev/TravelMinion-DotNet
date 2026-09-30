namespace TravelMinion.Domain;

/// <summary>
/// An itinerary day devoted to moving between destinations, with a travel leg
/// plus an optional lighter afternoon activity.
/// </summary>
public sealed class TravelDay : ItineraryDay
{
    public TravelDay(DateOnly date, string destination, TravelLeg travelLeg, TimeBlock? afternoonActivity = null)
        : base(date, destination)
    {
        ArgumentNullException.ThrowIfNull(travelLeg);

        TravelLeg = travelLeg;
        AfternoonActivity = afternoonActivity;
    }

    public override ItineraryDayType DayType => ItineraryDayType.Travel;

    public TravelLeg TravelLeg { get; }

    public TimeBlock? AfternoonActivity { get; }
}
