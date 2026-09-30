namespace TravelMinion.Domain;

/// <summary>
/// A time-blocked day-by-day plan built from the Approved Activity List,
/// organized as Activity Days, Travel Days, and Free Days.
/// </summary>
public sealed class Itinerary
{
    private readonly List<ItineraryDay> _days;

    public Itinerary(IEnumerable<ItineraryDay>? days = null)
        => _days = days?.ToList() ?? new List<ItineraryDay>();

    public IReadOnlyList<ItineraryDay> Days => _days;

    public ItineraryDay? GetDay(DateOnly date)
        => _days.FirstOrDefault(day => day.Date == date);

    /// <summary>The date range covered by this itinerary, or null when empty.</summary>
    public (DateOnly Start, DateOnly End)? DateRange
        => _days.Count == 0
            ? null
            : (_days.Min(day => day.Date), _days.Max(day => day.Date));

    public void Add(ItineraryDay day)
    {
        ArgumentNullException.ThrowIfNull(day);
        _days.Add(day);
    }
}
