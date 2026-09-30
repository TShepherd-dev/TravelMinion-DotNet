namespace TravelMinion.Domain;

/// <summary>
/// A single entry in a Trip's Approved Activity List. May originate from a
/// Suggestion or be added by the traveller, and carries the traveller's
/// curation (<see cref="Approved"/>, <see cref="Optional"/>, <see cref="Notes"/>).
/// </summary>
public sealed class ApprovedActivity
{
    public ApprovedActivity(
        string name,
        string area,
        string destination,
        string typicalDuration,
        string? rationale = null,
        string? openingHours = null,
        string? approximateCost = null,
        string? seasonWeatherFit = null,
        string? sourceLink = null,
        ResearchSourceName? foundVia = null,
        ConfidenceLevel? confidence = null,
        string? couldntVerify = null,
        string? notes = null,
        bool approved = true,
        bool optional = true,
        ActivityOrigin origin = ActivityOrigin.Manual,
        string? indoorFallback = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("An approved activity must have a name.");
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new DomainException("An approved activity must belong to a destination.");
        }

        Name = name.Trim();
        Area = area;
        Destination = destination.Trim();
        TypicalDuration = typicalDuration;
        Rationale = rationale;
        OpeningHours = openingHours;
        ApproximateCost = approximateCost;
        SeasonWeatherFit = seasonWeatherFit;
        SourceLink = sourceLink;
        FoundVia = foundVia;
        Confidence = confidence;
        CouldntVerify = couldntVerify;
        Notes = notes;
        Approved = approved;
        Optional = optional;
        Origin = origin;
        IndoorFallback = indoorFallback;
    }

    public string Name { get; }

    public string Area { get; }

    public string Destination { get; }

    public string TypicalDuration { get; }

    public string? Rationale { get; }

    public string? OpeningHours { get; }

    public string? ApproximateCost { get; }

    public string? SeasonWeatherFit { get; }

    public string? SourceLink { get; }

    public ResearchSourceName? FoundVia { get; }

    public ConfidenceLevel? Confidence { get; }

    public string? CouldntVerify { get; }

    public string? Notes { get; }

    /// <summary>Whether this activity is included in planning at all.</summary>
    public bool Approved { get; }

    /// <summary>
    /// True = filler (nice-to-have), false = must-do. Invariant 2: the planner
    /// schedules must-dos before fillers.
    /// </summary>
    public bool Optional { get; }

    public ActivityOrigin Origin { get; }

    public string? IndoorFallback { get; }

    public bool IsMustDo => !Optional;

    /// <summary>
    /// True when the traveller has discarded this activity so it is hidden from
    /// the list. Discarded activities are kept (soft-deleted) rather than removed.
    /// </summary>
    public bool Discarded { get; internal set; }

    internal void SetDiscarded(bool discarded) => Discarded = discarded;

    /// <summary>
    /// Creates an Approved Activity from a researched Suggestion, carrying the
    /// research context across.
    /// </summary>
    public static ApprovedActivity FromSuggestion(
        Suggestion suggestion,
        bool approved = true,
        bool optional = true,
        string? notes = null,
        string? indoorFallback = null)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        return new ApprovedActivity(
            suggestion.Name,
            suggestion.Area,
            suggestion.Destination,
            suggestion.TypicalDuration,
            rationale: suggestion.Rationale,
            openingHours: suggestion.OpeningHours,
            approximateCost: suggestion.ApproximateCost,
            seasonWeatherFit: suggestion.SeasonWeatherFit,
            sourceLink: suggestion.SourceLink,
            foundVia: suggestion.SourceName,
            confidence: suggestion.Confidence,
            couldntVerify: suggestion.CouldntVerify,
            notes: notes,
            approved: approved,
            optional: optional,
            origin: ActivityOrigin.Research,
            indoorFallback: indoorFallback);
    }
}
