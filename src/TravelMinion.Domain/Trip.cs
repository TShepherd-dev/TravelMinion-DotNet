namespace TravelMinion.Domain;

/// <summary>
/// One traveller's trip, and the aggregate root that owns everything about it:
/// its Trip Brief, Research Jobs, Approved Activity List, and Itinerary.
/// </summary>
public sealed class Trip
{
    private readonly List<ResearchJob> _researchJobs = new();
    private readonly List<Suggestion> _suggestions = new();

    public Trip(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A Trip requires a name.");
        }

        Id = id;
        Name = name.Trim();
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public TripBrief? Brief { get; private set; }

    public IReadOnlyList<ResearchJob> ResearchJobs => _researchJobs;

    /// <summary>The Trip's current Suggestions (the latest successful job's output).</summary>
    public IReadOnlyList<Suggestion> Suggestions => _suggestions;

    public ApprovedActivityList Activities { get; private set; } = new();

    public Itinerary? Itinerary { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A Trip requires a name.");
        }

        Name = name.Trim();
    }

    public void CaptureBrief(TripBrief brief)
    {
        ArgumentNullException.ThrowIfNull(brief);
        Brief = brief;
    }

    public ResearchJob QueueResearch(DateTimeOffset now, int attempt = 0)
    {
        var job = ResearchJob.Queue(Id, now, attempt);
        _researchJobs.Add(job);
        return job;
    }

    /// <summary>
    /// Replaces the current Suggestions. Called when a Research Job succeeds and
    /// the Trip is in Replace mode; the new job's output supersedes the prior set.
    /// </summary>
    public void ReplaceSuggestions(IEnumerable<Suggestion> suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        _suggestions.Clear();
        _suggestions.AddRange(suggestions);
    }

    /// <summary>
    /// Appends Suggestions that are new to the Trip, so the list grows rather than
    /// being replaced. A candidate is a duplicate (and skipped) when a Suggestion
    /// with the same name and destination already exists - including a discarded
    /// one, so research never resurrects something the traveller threw away.
    /// Returns the Suggestions actually appended.
    /// </summary>
    public IReadOnlyList<Suggestion> AppendSuggestions(IEnumerable<Suggestion> suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);

        var existing = new HashSet<(string Name, string Destination)>(
            _suggestions.Select(s => (s.Name.ToLowerInvariant(), s.Destination.ToLowerInvariant())));

        var appended = new List<Suggestion>();
        foreach (var suggestion in suggestions)
        {
            if (suggestion is null)
            {
                continue;
            }

            if (existing.Add((suggestion.Name.ToLowerInvariant(), suggestion.Destination.ToLowerInvariant())))
            {
                _suggestions.Add(suggestion);
                appended.Add(suggestion);
            }
        }

        return appended;
    }

    /// <summary>Marks the suggestions at the given indices as discarded (soft delete).</summary>
    public void DiscardSuggestions(IEnumerable<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);

        foreach (var index in indices)
        {
            if (index >= 0 && index < _suggestions.Count)
            {
                _suggestions[index].Discarded = true;
            }
        }
    }

    public void SetActivities(ApprovedActivityList activities)
    {
        ArgumentNullException.ThrowIfNull(activities);
        Activities = activities;
    }

    public void SetItinerary(Itinerary itinerary)
    {
        ArgumentNullException.ThrowIfNull(itinerary);
        Itinerary = itinerary;
    }
}
