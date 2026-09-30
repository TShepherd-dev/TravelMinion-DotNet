namespace TravelMinion.Application;

/// <summary>
/// Port for the LLM-backed enrichment of a research hit. Turns a raw hit into
/// the rationale, area, duration, hours, cost, and season fit a Suggestion
/// needs. Backed by an LLM in production and a deterministic fake in tests.
/// </summary>
public interface IResearchEnricher
{
    Task<SuggestionEnrichment> EnrichAsync(
        RawResult raw,
        IReadOnlyList<string> interests,
        string destination,
        CancellationToken cancellationToken = default);
}
