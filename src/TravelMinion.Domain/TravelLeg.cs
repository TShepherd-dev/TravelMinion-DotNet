namespace TravelMinion.Domain;

/// <summary>
/// A single move between two destinations, embedded as a block inside a day
/// rather than a standalone day.
/// </summary>
public sealed class TravelLeg
{
    public TravelLeg(string fromDestination, string toDestination, string? mode = null, string? duration = null)
    {
        if (string.IsNullOrWhiteSpace(fromDestination))
        {
            throw new DomainException("A travel leg must have an origin.");
        }

        if (string.IsNullOrWhiteSpace(toDestination))
        {
            throw new DomainException("A travel leg must have a destination.");
        }

        FromDestination = fromDestination.Trim();
        ToDestination = toDestination.Trim();
        Mode = mode;
        Duration = duration;
    }

    public string FromDestination { get; }

    public string ToDestination { get; }

    public string? Mode { get; }

    public string? Duration { get; }
}
