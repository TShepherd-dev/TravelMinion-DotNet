namespace TravelMinion.Domain;

/// <summary>
/// The persisted capture of the clarifying interview: destinations, dates,
/// interests, travel style, and other inputs.
/// </summary>
public sealed class TripBrief
{
    private readonly List<DestinationStop> _destinations;

    private TripBrief()
    {
        _destinations = new List<DestinationStop>();
        Interests = new List<string>();
        Dietary = new List<string>();
        PreferredSources = new List<string>();
        TravellersToShare = new List<string>();
    }

    private TripBrief(
        List<DestinationStop> destinations,
        DateOnly startDate,
        DateOnly endDate,
        List<string> interests,
        TravelStyle travelStyle,
        string? budget,
        int? groupSize,
        string? mobility,
        List<string> dietary,
        List<string> preferredSources,
        List<string> travellersToShare)
    {
        _destinations = destinations;
        StartDate = startDate;
        EndDate = endDate;
        Interests = interests;
        TravelStyle = travelStyle;
        Budget = budget;
        GroupSize = groupSize;
        Mobility = mobility;
        Dietary = dietary;
        PreferredSources = preferredSources;
        TravellersToShare = travellersToShare;
    }

    public IReadOnlyList<DestinationStop> Destinations => _destinations;

    public DateOnly StartDate { get; }

    public DateOnly EndDate { get; }

    public IReadOnlyList<string> Interests { get; }

    public TravelStyle TravelStyle { get; }

    public string? Budget { get; }

    public int? GroupSize { get; }

    public string? Mobility { get; }

    public IReadOnlyList<string> Dietary { get; }

    public IReadOnlyList<string> PreferredSources { get; }

    public IReadOnlyList<string> TravellersToShare { get; }

    /// <summary>Number of days the trip spans, inclusive of both ends.</summary>
    public int SpanInDays => EndDate.DayNumber - StartDate.DayNumber + 1;

    public static TripBrief Create(
        IEnumerable<DestinationStop> destinations,
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<string>? interests = null,
        TravelStyle travelStyle = TravelStyle.Casual,
        string? budget = null,
        int? groupSize = null,
        string? mobility = null,
        IEnumerable<string>? dietary = null,
        IEnumerable<string>? preferredSources = null,
        IEnumerable<string>? travellersToShare = null)
    {
        ArgumentNullException.ThrowIfNull(destinations);

        var stops = destinations.ToList();
        if (stops.Count == 0)
        {
            throw new DomainException("A Trip Brief requires at least one destination.");
        }

        if (endDate < startDate)
        {
            throw new DomainException("A Trip Brief's end date must not be before its start date.");
        }

        if (groupSize is < 1)
        {
            throw new DomainException("A Trip Brief's group size must be at least one.");
        }

        var interestList = NormaliseList(interests);
        if (interestList.Count == 0)
        {
            interestList = DomainDefaults.Interests.ToList();
        }

        var brief = new TripBrief(
            stops,
            startDate,
            endDate,
            interestList,
            travelStyle,
            NormaliseText(budget),
            groupSize,
            NormaliseText(mobility),
            NormaliseList(dietary),
            NormaliseList(preferredSources),
            NormaliseList(travellersToShare));

        brief.DistributeDays();
        return brief;
    }

    /// <summary>
    /// Invariant 3: destination days must sum to the trip span. When they do
    /// not, days are redistributed evenly with the remainder going to earlier
    /// stops.
    /// </summary>
    private void DistributeDays()
    {
        var total = SpanInDays;
        if (total < 1 || _destinations.Count == 0)
        {
            return;
        }

        if (_destinations.Sum(stop => stop.Days) == total)
        {
            return;
        }

        var baseDays = total / _destinations.Count;
        var remainder = total % _destinations.Count;
        for (var i = 0; i < _destinations.Count; i++)
        {
            _destinations[i].Days = baseDays + (i < remainder ? 1 : 0);
        }
    }

    private static List<string> NormaliseList(IEnumerable<string>? values)
        => values is null
            ? new List<string>()
            : values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList();

    private static string? NormaliseText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
