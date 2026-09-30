namespace TravelMinion.Application;

/// <summary>
/// Port for fetching the full content of a URL as Markdown (Jina AI Reader in
/// production). Returns <c>null</c> when the page cannot be fetched.
/// </summary>
public interface IUrlFetcher
{
    Task<string?> FetchAsync(string url, CancellationToken cancellationToken = default);
}
