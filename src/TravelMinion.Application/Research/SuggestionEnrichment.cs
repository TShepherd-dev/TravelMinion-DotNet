namespace TravelMinion.Application;

/// <summary>
/// The Suggestion fields the LLM is responsible for deriving from a research
/// hit: why it matches the traveller's interests, its area, its duration, and
/// its hours, cost, and season/weather fit.
/// </summary>
public sealed record SuggestionEnrichment(
    string Rationale,
    string Area,
    string TypicalDuration,
    string? OpeningHours = null,
    string? ApproximateCost = null,
    string? SeasonWeatherFit = null);
