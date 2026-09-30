using System.Net;
using System.Text.RegularExpressions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure;

/// <summary>
/// Zero-key fallback research source that scrapes DuckDuckGo's HTML endpoint.
/// Best-effort: mirrors the Python prototype's <c>DuckDuckGoSource</c>.
/// </summary>
public sealed partial class DuckDuckGoResearchSource : IResearchSource
{
    private const int MaxInterestQueries = 2;
    private const string Endpoint = "https://html.duckduckgo.com/html/";

    private readonly HttpClient _httpClient;

    /// <summary>Creates the source.</summary>
    public DuckDuckGoResearchSource(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
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
            var html = await QueryAsync(query, cancellationToken).ConfigureAwait(false);
            if (html is not null)
            {
                results.AddRange(ParseResults(html));
            }
        }

        return results;
    }

    /// <summary>Builds the fallback queries for a destination.</summary>
    internal static IReadOnlyList<string> BuildQueries(string destination, IReadOnlyList<string> interests)
    {
        var queries = interests
            .Take(MaxInterestQueries)
            .Select(interest => $"best {interest} {destination} tourist attraction")
            .ToList();
        queries.Add($"top attractions {destination} travel guide");
        return queries;
    }

    /// <summary>Parses DuckDuckGo HTML search results into raw results.</summary>
    internal static IReadOnlyList<RawResult> ParseResults(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<RawResult>();
        }

        var snippets = SnippetRegex().Matches(html).Select(match => Clean(match.Groups["snippet"].Value)).ToList();
        var results = new List<RawResult>();
        var index = 0;
        foreach (Match match in ResultLinkRegex().Matches(html))
        {
            var title = Clean(match.Groups["title"].Value);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var url = NormalizeUrl(match.Groups["href"].Value);
            var snippet = index < snippets.Count ? snippets[index] : null;
            results.Add(new RawResult(title, url, snippet, ResearchSourceName.DuckDuckGo));
            index++;
        }

        return results;
    }

    private async Task<string?> QueryAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["q"] = query });
            using var response = await _httpClient
                .PostAsync(Endpoint, content, cancellationToken)
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
            // HttpClient timeout (not caller cancellation): skip this query.
            return null;
        }
    }

    private static string NormalizeUrl(string href)
    {
        var decoded = WebUtility.HtmlDecode(href);
        var match = RedirectRegex().Match(decoded);
        if (match.Success)
        {
            return Uri.UnescapeDataString(match.Groups["url"].Value);
        }

        return decoded.StartsWith("//", StringComparison.Ordinal) ? "https:" + decoded : decoded;
    }

    private static string Clean(string value) =>
        WebUtility.HtmlDecode(TagRegex().Replace(value, string.Empty)).Trim();

    [GeneratedRegex(
        "<a[^>]*class=\"[^\"]*result__a[^\"]*\"[^>]*href=\"(?<href>[^\"]+)\"[^>]*>(?<title>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ResultLinkRegex();

    [GeneratedRegex(
        "<a[^>]*class=\"[^\"]*result__snippet[^\"]*\"[^>]*>(?<snippet>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SnippetRegex();

    [GeneratedRegex("uddg=(?<url>[^&]+)", RegexOptions.IgnoreCase)]
    private static partial Regex RedirectRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();
}
