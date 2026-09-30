namespace TravelMinion.Domain;

/// <summary>
/// The human-owned, living list for a Trip. Sole source for itinerary planning.
/// </summary>
public sealed class ApprovedActivityList
{
    private readonly List<ApprovedActivity> _activities;

    public ApprovedActivityList(IEnumerable<ApprovedActivity>? activities = null)
        => _activities = activities?.ToList() ?? new List<ApprovedActivity>();

    public IReadOnlyList<ApprovedActivity> Activities => _activities;

    public IEnumerable<ApprovedActivity> ApprovedOnly()
        => _activities.Where(activity => activity.Approved);

    /// <summary>Approved activities that are must-dos.</summary>
    public IEnumerable<ApprovedActivity> MustDo()
        => ApprovedOnly().Where(activity => activity.IsMustDo);

    /// <summary>Approved activities that are fillers.</summary>
    public IEnumerable<ApprovedActivity> Fillers()
        => ApprovedOnly().Where(activity => !activity.IsMustDo);

    /// <summary>
    /// Approved activities for a destination, must-dos first (Invariant 2).
    /// </summary>
    public IEnumerable<ApprovedActivity> ByDestination(string destination)
        => ApprovedOnly()
            .Where(activity => string.Equals(activity.Destination, destination, StringComparison.OrdinalIgnoreCase))
            .OrderBy(activity => activity.Optional);

    public void Add(ApprovedActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activities.Add(activity);
    }

    public void Remove(ApprovedActivity activity) => _activities.Remove(activity);

    /// <summary>Marks an activity as discarded by the traveller (a soft delete).</summary>
    public void Discard(ApprovedActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.SetDiscarded(true);
    }

    public void Replace(IEnumerable<ApprovedActivity> activities)
    {
        ArgumentNullException.ThrowIfNull(activities);
        _activities.Clear();
        _activities.AddRange(activities);
    }
}
