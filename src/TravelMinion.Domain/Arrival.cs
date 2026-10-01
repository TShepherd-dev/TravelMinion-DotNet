namespace TravelMinion.Domain;

/// <summary>
/// The point at which a Trip begins: the <see cref="Base"/> where the traveller
/// first arrives (identified by its unique name), plus the date and time of
/// arrival.
/// </summary>
public sealed record Arrival
{
    public Arrival(string baseName, DateOnly date, TimeOnly time)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new DomainException("An arrival must name the base where the traveller arrives.");
        }

        BaseName = baseName.Trim();
        Date = date;
        Time = time;
    }

    /// <summary>The name of the base where the traveller arrives.</summary>
    public string BaseName { get; }

    public DateOnly Date { get; }

    public TimeOnly Time { get; }
}
