namespace TravelMinion.Domain;

/// <summary>
/// A single researched attraction/activity candidate, carrying details such as
/// hours, cost, duration, area, and season/weather fit.
/// </summary>
public sealed class Suggestion
{
    public Suggestion(
        string name,
        string destination,
        string rationale,
        string area,
        string typicalDuration,
        string? openingHours = null,
        string? approximateCost = null,
        string? seasonWeatherFit = null,
        string? sourceLink = null,
        ResearchSourceName sourceName = ResearchSourceName.Tavily,
        ConfidenceLevel confidence = ConfidenceLevel.Medium,
        string? couldntVerify = null,
        bool discarded = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A suggestion must name an activity.");
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new DomainException("A suggestion must belong to a destination.");
        }

        Name = name.Trim();
        Destination = destination.Trim();
        Rationale = rationale;
        Area = area;
        TypicalDuration = typicalDuration;
        OpeningHours = openingHours;
        ApproximateCost = approximateCost;
        SeasonWeatherFit = seasonWeatherFit;
        SourceLink = sourceLink;
        SourceName = sourceName;
        Confidence = confidence;
        CouldntVerify = couldntVerify;
        Discarded = discarded;
    }

    public string Name { get; }

    public string Destination { get; }

    public string Rationale { get; }

    public string Area { get; }

    public string TypicalDuration { get; }

    public string? OpeningHours { get; }

    public string? ApproximateCost { get; }

    public string? SeasonWeatherFit { get; }

    public string? SourceLink { get; }

    public ResearchSourceName SourceName { get; }

    public ConfidenceLevel Confidence { get; }

    public string? CouldntVerify { get; }

    /// <summary>
    /// True when the traveller has discarded this candidate so it is hidden from
    /// the list. Discarded Suggestions are remembered (and excluded from later
    /// merges) rather than deleted, so research never resurrects them.
    /// </summary>
    public bool Discarded { get; internal set; }
}
