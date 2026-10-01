using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>The outcome of starting an interview: a draft plus any follow-ups.</summary>
public sealed record InterviewResult(TripBriefDraft Draft, IReadOnlyList<string> Questions)
{
    public bool NeedsMoreInformation => Questions.Count > 0;
}

/// <summary>
/// Drives the clarifying interview: extracts a draft Trip Brief from free text,
/// reports the required fields still missing, and finalises a validated
/// <see cref="TripBrief"/> with defaults for anything left blank.
/// </summary>
public sealed class InterviewService
{
    private readonly ITripBriefExtractor _extractor;

    public InterviewService(ITripBriefExtractor extractor)
        => _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));

    public async Task<InterviewResult> StartAsync(string freeform, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(freeform))
        {
            throw new ArgumentException("A trip description is required.", nameof(freeform));
        }

        var draft = await _extractor.ExtractAsync(freeform, cancellationToken).ConfigureAwait(false);
        return new InterviewResult(draft, BuildQuestions(draft));
    }

    /// <summary>
    /// Builds the bounded follow-up questions for the required fields a draft
    /// is still missing.
    /// </summary>
    public static IReadOnlyList<string> BuildQuestions(TripBriefDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var missing = draft.MissingRequiredFields();
        var questions = new List<string>();

        if (missing.Contains("destinations"))
        {
            questions.Add(
                "Where do you want to travel? " +
                "(e.g., 'Tokyo and Kyoto, Japan' or 'Paris, France')");
        }

        var missingStart = missing.Contains("start_date");
        var missingEnd = missing.Contains("end_date");
        if (missingStart && missingEnd)
        {
            questions.Add(
                "What are your travel dates? Please provide both start and end " +
                "dates (e.g., '2027-04-01 to 2027-04-10' or 'April 1-10, 2027').");
        }
        else if (missingStart)
        {
            questions.Add("When does your trip start? (e.g., '2027-04-01' or 'April 1, 2027')");
        }
        else if (missingEnd)
        {
            questions.Add("When does your trip end? (e.g., '2027-04-10' or 'April 10, 2027')");
        }

        if (missing.Contains("interests"))
        {
            questions.Add(
                "What are you interested in experiencing? " +
                "(e.g., 'local culture, food, history, nature' - or I can use defaults)");
        }

        if (missing.Contains("travel_style"))
        {
            questions.Add(
                "How packed do you want your days? Choose: 'packed' (5-6 activities/day), " +
                "'casual' (2-3/day), or 'nothing' (mostly rest/free days)");
        }

        return questions;
    }

    /// <summary>
    /// Finalises a validated <see cref="TripBrief"/> from a draft. Missing dates
    /// fall back to a placeholder six months out; missing destinations become
    /// "TBD"; interests and travel style fall back to their defaults.
    /// </summary>
    public static TripBrief Finalize(TripBriefDraft draft, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var destinations = draft.Destinations.Count > 0
            ? draft.Destinations
            : new[] { new DestinationDraft("TBD") };

        var countries = FlatGeography.ToCountries(
            destinations.Select(destination => (destination.Destination, destination.Days ?? 1)));

        var startDate = draft.StartDate ?? today.AddDays(180);
        var endDate = draft.EndDate ?? startDate.AddDays(7);

        var arrival = new Arrival(countries[0].FirstBase.Name, startDate, TimeOnly.MinValue);
        var departure = new Departure(countries[^1].LastBase.Name, endDate, TimeOnly.MinValue);

        return TripBrief.Create(
            countries,
            arrival,
            departure,
            draft.Interests.Count > 0 ? draft.Interests : null,
            draft.TravelStyle ?? TravelStyle.Casual,
            draft.Budget,
            draft.GroupSize,
            draft.Mobility,
            draft.Dietary,
            draft.PreferredSources,
            draft.TravellersToShare);
    }
}
