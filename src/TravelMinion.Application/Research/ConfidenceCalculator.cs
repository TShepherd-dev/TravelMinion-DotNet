using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Deterministic confidence scoring, ported from the Python prototype's
/// <c>_calculate_confidence</c>: high when hours, pricing, and a source link
/// are all present; medium when one is missing; low when two or more are.
/// </summary>
internal static class ConfidenceCalculator
{
    public static (ConfidenceLevel Confidence, string? CouldntVerify) Calculate(
        SuggestionEnrichment enrichment,
        string? sourceLink)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(enrichment.OpeningHours))
        {
            missing.Add("opening hours");
        }

        if (string.IsNullOrWhiteSpace(enrichment.ApproximateCost))
        {
            missing.Add("pricing");
        }

        if (string.IsNullOrWhiteSpace(sourceLink))
        {
            missing.Add("source link");
        }

        return missing.Count switch
        {
            0 => (ConfidenceLevel.High, null),
            1 => (ConfidenceLevel.Medium, $"Couldn't verify {missing[0]}"),
            _ => (ConfidenceLevel.Low, $"Couldn't verify {string.Join(", ", missing)}"),
        };
    }
}
