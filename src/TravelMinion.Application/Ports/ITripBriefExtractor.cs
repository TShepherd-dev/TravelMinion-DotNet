using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Port for turning a traveller's free-form trip description into a draft Trip
/// Brief. Backed by an LLM in production and a deterministic fake in tests.
/// </summary>
public interface ITripBriefExtractor
{
    Task<TripBriefDraft> ExtractAsync(string freeform, CancellationToken cancellationToken = default);
}
