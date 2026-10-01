namespace TravelMinion.Domain;

/// <summary>
/// The point at which a Trip ends: the <see cref="Base"/> from which the
/// traveller finally departs (identified by its unique name), plus the date and
/// time of departure.
/// </summary>
public sealed record Departure
{
    public Departure(string baseName, DateOnly date, TimeOnly time)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new DomainException("A departure must name the base the traveller departs from.");
        }

        BaseName = baseName.Trim();
        Date = date;
        Time = time;
    }

    /// <summary>The name of the base the traveller departs from.</summary>
    public string BaseName { get; }

    public DateOnly Date { get; }

    public TimeOnly Time { get; }
}
