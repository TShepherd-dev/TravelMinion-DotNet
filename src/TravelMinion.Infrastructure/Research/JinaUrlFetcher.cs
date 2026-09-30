using Microsoft.Extensions.Options;
using TravelMinion.Application;

namespace TravelMinion.Infrastructure;

/// <summary>
/// Fetches page content as Markdown via the Jina AI Reader.
/// Used to enrich search results and to read traveller-preferred source URLs.
/// </summary>
public sealed class JinaUrlFetcher : IUrlFetcher
{
    private readonly HttpClient _httpClient;
    private readonly JinaOptions _options;

    /// <summary>Creates the fetcher.</summary>
    public JinaUrlFetcher(HttpClient httpClient, IOptions<JinaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            using var response = await _httpClient
                .GetAsync(_options.BaseUrl + url, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout (not caller cancellation): treat as a soft failure,
            // mirroring the prototype's httpx timeout handling.
            return null;
        }
    }
}
