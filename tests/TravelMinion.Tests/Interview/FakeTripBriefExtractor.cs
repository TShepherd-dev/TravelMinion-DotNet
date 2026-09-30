using TravelMinion.Application;

namespace TravelMinion.Tests;

/// <summary>
/// Deterministic stand-in for the LLM-backed extractor, used to keep the
/// interview tests hermetic.
/// </summary>
internal sealed class FakeTripBriefExtractor : ITripBriefExtractor
{
    private readonly TripBriefDraft _draft;

    public FakeTripBriefExtractor(TripBriefDraft draft) => _draft = draft;

    public string? LastFreeform { get; private set; }

    public Task<TripBriefDraft> ExtractAsync(string freeform, CancellationToken cancellationToken = default)
    {
        LastFreeform = freeform;
        return Task.FromResult(_draft);
    }
}
