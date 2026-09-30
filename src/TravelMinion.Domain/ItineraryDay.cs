namespace TravelMinion.Domain;

/// <summary>
/// Base type for an itinerary day. Concrete days are Activity, Travel, or Free.
/// </summary>
public abstract class ItineraryDay
{
    protected ItineraryDay(DateOnly date, string destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new DomainException("An itinerary day must belong to a destination.");
        }

        Date = date;
        Destination = destination.Trim();
    }

    public DateOnly Date { get; }

    public string Destination { get; }

    public abstract ItineraryDayType DayType { get; }
}
