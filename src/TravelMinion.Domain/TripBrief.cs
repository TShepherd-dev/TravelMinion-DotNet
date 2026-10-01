namespace TravelMinion.Domain;

/// <summary>
/// The persisted capture of the clarifying interview: the country/base
/// geography, arrival and departure, interests, travel style, and other inputs.
/// </summary>
public sealed class TripBrief
{
    private readonly List<Country> _countries;
    private IReadOnlyList<Base>? _bases;

    private TripBrief()
    {
        _countries = new List<Country>();
        Interests = new List<string>();
        Dietary = new List<string>();
        PreferredSources = new List<string>();
        TravellersToShare = new List<string>();
        Arrival = null!;
        Departure = null!;
    }

    private TripBrief(
        List<Country> countries,
        Arrival arrival,
        Departure departure,
        List<string> interests,
        TravelStyle travelStyle,
        string? budget,
        int? groupSize,
        string? mobility,
        List<string> dietary,
        List<string> preferredSources,
        List<string> travellersToShare)
    {
        _countries = countries;
        Arrival = arrival;
        Departure = departure;
        Interests = interests;
        TravelStyle = travelStyle;
        Budget = budget;
        GroupSize = groupSize;
        Mobility = mobility;
        Dietary = dietary;
        PreferredSources = preferredSources;
        TravellersToShare = travellersToShare;
    }

    /// <summary>The ordered countries visited, each owning an ordered chain of bases.</summary>
    public IReadOnlyList<Country> Countries => _countries;

    /// <summary>
    /// Every base across all countries, flattened into trip order. This is the
    /// chain the planner and research iterate.
    /// </summary>
    public IReadOnlyList<Base> Bases =>
        _bases ??= _countries.SelectMany(country => country.Bases).ToList();

    /// <summary>Where and when the trip begins.</summary>
    public Arrival Arrival { get; }

    /// <summary>Where and when the trip ends.</summary>
    public Departure Departure { get; }

    /// <summary>Derived from <see cref="Arrival"/>.</summary>
    public DateOnly StartDate => Arrival.Date;

    /// <summary>Derived from <see cref="Departure"/>.</summary>
    public DateOnly EndDate => Departure.Date;

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
        IEnumerable<Country> countries,
        Arrival arrival,
        Departure departure,
        IEnumerable<string>? interests = null,
        TravelStyle travelStyle = TravelStyle.Casual,
        string? budget = null,
        int? groupSize = null,
        string? mobility = null,
        IEnumerable<string>? dietary = null,
        IEnumerable<string>? preferredSources = null,
        IEnumerable<string>? travellersToShare = null)
    {
        ArgumentNullException.ThrowIfNull(countries);
        ArgumentNullException.ThrowIfNull(arrival);
        ArgumentNullException.ThrowIfNull(departure);

        var countryList = countries.ToList();
        if (countryList.Count == 0)
        {
            throw new DomainException("A Trip Brief requires at least one country.");
        }

        if (departure.Date < arrival.Date)
        {
            throw new DomainException("A Trip Brief's departure must not be before its arrival.");
        }

        if (groupSize is < 1)
        {
            throw new DomainException("A Trip Brief's group size must be at least one.");
        }

        EnsureUniqueNames(countryList);
        EnsureEndpointsMatch(countryList, arrival, departure);

        var interestList = NormaliseList(interests);
        if (interestList.Count == 0)
        {
            interestList = DomainDefaults.Interests.ToList();
        }

        var brief = new TripBrief(
            countryList,
            arrival,
            departure,
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
    /// Invariant 3: country spans must sum to the trip span. When they do not,
    /// spans are redistributed evenly with the remainder going to earlier
    /// countries, and each country redistributes its bases to match.
    /// </summary>
    private void DistributeDays()
    {
        var total = SpanInDays;
        if (total < 1 || _countries.Count == 0)
        {
            return;
        }

        if (total < _countries.Count)
        {
            throw new DomainException("A trip must span at least one day per country.");
        }

        if (_countries.Sum(country => country.SpanInDays) == total)
        {
            return;
        }

        var spans = DayDistribution.Distribute(total, _countries.Count);
        for (var i = 0; i < _countries.Count; i++)
        {
            _countries[i].SetSpan(spans[i]);
        }
    }

    /// <summary>
    /// Invariant 5: country names are unique within a Trip, and base names are
    /// unique within a Trip (activities key off base name).
    /// </summary>
    private static void EnsureUniqueNames(List<Country> countries)
    {
        var countryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var baseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var country in countries)
        {
            if (!countryNames.Add(country.Name))
            {
                throw new DomainException(
                    $"Country names must be unique within a Trip; '{country.Name}' appears more than once.");
            }

            foreach (var @base in country.Bases)
            {
                if (!baseNames.Add(@base.Name))
                {
                    throw new DomainException(
                        $"Base names must be unique within a Trip; '{@base.Name}' appears more than once.");
                }
            }
        }
    }

    /// <summary>
    /// The trip must arrive at the first base of the first country and depart
    /// from the last base of the last country.
    /// </summary>
    private static void EnsureEndpointsMatch(
        List<Country> countries,
        Arrival arrival,
        Departure departure)
    {
        var firstBase = countries[0].FirstBase;
        if (!string.Equals(arrival.BaseName, firstBase.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(
                "A Trip Brief must arrive at the first base of the first country.");
        }

        var lastBase = countries[^1].LastBase;
        if (!string.Equals(departure.BaseName, lastBase.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(
                "A Trip Brief must depart from the last base of the last country.");
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
