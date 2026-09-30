using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>A destination named in a draft Trip Brief, before days are assigned.</summary>
public sealed record DestinationDraft(string Destination, int? Days = null);

/// <summary>
/// The partially-filled result of extracting a Trip Brief from free text. Any
/// required field the traveller did not supply is left null/empty and reported
/// by <see cref="MissingRequiredFields"/>.
/// </summary>
public sealed record TripBriefDraft
{
    public IReadOnlyList<DestinationDraft> Destinations { get; init; } = Array.Empty<DestinationDraft>();

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public IReadOnlyList<string> Interests { get; init; } = Array.Empty<string>();

    public TravelStyle? TravelStyle { get; init; }

    public string? Budget { get; init; }

    public int? GroupSize { get; init; }

    public string? Mobility { get; init; }

    public IReadOnlyList<string> Dietary { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> TravellersToShare { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PreferredSources { get; init; } = Array.Empty<string>();

    public bool IsComplete => MissingRequiredFields().Count == 0;

    public IReadOnlyList<string> MissingRequiredFields()
    {
        var missing = new List<string>();
        if (Destinations.Count == 0)
        {
            missing.Add("destinations");
        }

        if (StartDate is null)
        {
            missing.Add("start_date");
        }

        if (EndDate is null)
        {
            missing.Add("end_date");
        }

        if (Interests.Count == 0)
        {
            missing.Add("interests");
        }

        if (TravelStyle is null)
        {
            missing.Add("travel_style");
        }

        return missing;
    }
}
