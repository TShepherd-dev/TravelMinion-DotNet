namespace TravelMinion.Application;

/// <summary>
/// Port for a search-backed research source (Tavily in production, DuckDuckGo
/// as the zero-key fallback). Jina is a URL fetcher rather than a searcher, so
/// it is modelled separately by <see cref="IUrlFetcher"/>.
/// </summary>
public interface IResearchSource
{
    Task<IReadOnlyList<RawResult>> SearchAsync(
        string destination,
        IReadOnlyList<string> interests,
        int days,
        CancellationToken cancellationToken = default);
}
