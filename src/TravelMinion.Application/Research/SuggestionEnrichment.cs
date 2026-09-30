namespace TravelMinion.Application;

/// <summary>
/// The Suggestion fields the LLM is responsible for deriving from a research
/// hit: the activity's name, why it matches the traveller's interests, its
/// area, its duration, and its hours, cost, and season/weather fit. A single
/// research hit (a list or guide page, say) may yield several of these.
/// </summary>
public sealed record SuggestionEnrichment(
    string Name,
    string Rationale,
    string Area,
    string TypicalDuration,
    string? OpeningHours = null,
    string? ApproximateCost = null,
    string? SeasonWeatherFit = null);
