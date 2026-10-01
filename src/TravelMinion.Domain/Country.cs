namespace TravelMinion.Domain;

/// <summary>
/// A country visited during a Trip. Owns an ordered chain of <see cref="Base"/>
/// values and carries the intended span of time spent there.
/// </summary>
public sealed class Country
{
    private readonly List<Base> _bases;

    /// <summary>Parameterless constructor for EF Core materialisation.</summary>
    private Country()
    {
        Name = null!;
        _bases = new List<Base>();
    }

    public Country(string name, int spanInDays, IEnumerable<Base> bases)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A country must have a name.");
        }

        if (spanInDays < 1)
        {
            throw new DomainException("A country must span at least one day.");
        }

        ArgumentNullException.ThrowIfNull(bases);

        var baseList = bases.ToList();
        if (baseList.Count == 0)
        {
            throw new DomainException("A country requires at least one base.");
        }

        Name = name.Trim();
        SpanInDays = spanInDays;
        _bases = baseList;
        DistributeDays();
    }

    /// <summary>The country name. Unique within a Trip.</summary>
    public string Name { get; }

    /// <summary>
    /// The intended number of days spent in this country. Bases are
    /// redistributed so their days sum to this span.
    /// </summary>
    public int SpanInDays { get; private set; }

    /// <summary>The ordered chain of bases visited within this country.</summary>
    public IReadOnlyList<Base> Bases => _bases;

    /// <summary>The first base in the country's chain.</summary>
    public Base FirstBase => _bases[0];

    /// <summary>The last base in the country's chain.</summary>
    public Base LastBase => _bases[^1];

    /// <summary>
    /// Sets a new span (used when the trip-level distribution changes it) and
    /// redistributes the bases to match.
    /// </summary>
    internal void SetSpan(int spanInDays)
    {
        SpanInDays = spanInDays;
        DistributeDays();
    }

    /// <summary>
    /// Invariant 3: a country's base days must sum to its span. When they do
    /// not, days are redistributed evenly with the remainder going to earlier
    /// bases.
    /// </summary>
    private void DistributeDays()
    {
        if (SpanInDays < _bases.Count)
        {
            throw new DomainException("A country must span at least one day per base.");
        }

        if (_bases.Sum(b => b.Days) == SpanInDays)
        {
            return;
        }

        var days = DayDistribution.Distribute(SpanInDays, _bases.Count);
        for (var i = 0; i < _bases.Count; i++)
        {
            _bases[i].Days = days[i];
        }
    }
}
