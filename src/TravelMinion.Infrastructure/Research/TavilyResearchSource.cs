using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure;

/// <summary>
/// Primary research source backed by the Tavily search API.
/// Queries per interest (top three) plus a general query, mirroring the
/// Python prototype's <c>TavilySource</c>.
/// </summary>
public sealed class TavilyResearchSource : IResearchSource
{
    private const int MaxResultsPerQuery = 5;
    private const int MaxInterestQueries = 3;

    private readonly HttpClient _httpClient;
    private readonly TavilyOptions _options;

    /// <summary>Creates the source.</summary>
    public TavilyResearchSource(HttpClient httpClient, IOptions<TavilyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RawResult>> SearchAsync(
        string destination,
        IReadOnlyList<string> interests,
        int days,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RawResult>();
        foreach (var query in BuildQueries(destination, interests))
        {
            results.AddRange(await QueryAsync(query, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <summary>Builds the interest-focused queries for a destination.</summary>
    internal static IReadOnlyList<string> BuildQueries(string destination, IReadOnlyList<string> interests)
    {
        var queries = interests
            .Take(MaxInterestQueries)
            .Select(interest => $"best {interest} in {destination} tourist attractions")
            .ToList();
        queries.Add($"top tourist attractions {destination} travel guide");
        return queries;
    }

    private async Task<IReadOnlyList<RawResult>> QueryAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var request = new TavilyRequest(query, "basic", true, MaxResultsPerQuery);
            using var response = await _httpClient
                .PostAsJsonAsync(_options.Endpoint, request, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<RawResult>();
            }

            var payload = await response.Content
                .ReadFromJsonAsync<TavilyResponse>(cancellationToken)
                .ConfigureAwait(false);
            if (payload?.Results is null)
            {
                return Array.Empty<RawResult>();
            }

            return payload.Results
                .Where(result => !string.IsNullOrWhiteSpace(result.Title))
                .Select(result => new RawResult(
                    result.Title!,
                    result.Url ?? string.Empty,
                    result.Content,
                    ResearchSourceName.Tavily))
                .ToList();
        }
        catch (HttpRequestException)
        {
            return Array.Empty<RawResult>();
        }
        catch (JsonException)
        {
            return Array.Empty<RawResult>();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout (not caller cancellation): skip this query.
            return Array.Empty<RawResult>();
        }
    }

    private sealed record TavilyRequest(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("search_depth")] string SearchDepth,
        [property: JsonPropertyName("include_answers")] bool IncludeAnswers,
        [property: JsonPropertyName("max_results")] int MaxResults);

    private sealed class TavilyResponse
    {
        [JsonPropertyName("results")]
        public List<TavilyResult>? Results { get; set; }
    }

    private sealed class TavilyResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
