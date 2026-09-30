namespace TravelMinion.Domain;

/// <summary>
/// A time-blocked activity within a day.
/// </summary>
public sealed class TimeBlock
{
    public TimeBlock(
        TimeOnly startTime,
        TimeOnly endTime,
        string activityName,
        string place,
        string duration,
        string? transitToNext = null,
        string? indoorFallback = null)
    {
        if (endTime < startTime)
        {
            throw new DomainException("A time block cannot end before it starts.");
        }

        if (string.IsNullOrWhiteSpace(activityName))
        {
            throw new DomainException("A time block must name an activity.");
        }

        StartTime = startTime;
        EndTime = endTime;
        ActivityName = activityName.Trim();
        Place = place;
        Duration = duration;
        TransitToNext = transitToNext;
        IndoorFallback = indoorFallback;
    }

    public TimeOnly StartTime { get; }

    public TimeOnly EndTime { get; }

    public string ActivityName { get; }

    public string Place { get; }

    public string Duration { get; }

    public string? TransitToNext { get; }

    public string? IndoorFallback { get; }
}
