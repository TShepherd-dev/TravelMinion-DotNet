using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Orchestrates the Research Step for a Trip: fetches the traveller's custom
/// sources, queries the primary search source (Tavily) with a fallback
/// (DuckDuckGo), enriches the hits with full page content, then shapes them
/// into Suggestions. Ported from the Python prototype's <c>ResearchEngine</c>.
/// </summary>
public sealed class ResearchEngine
{
    private const int MaxContentLength = 5000;
    private const int MaxConcurrency = 4;

    private readonly IResearchEnricher _enricher;
    private readonly IUrlFetcher _urlFetcher;
    private readonly IResearchSource _fallback;
    private readonly IResearchSource? _primary;

    public ResearchEngine(
        IResearchEnricher enricher,
        IUrlFetcher urlFetcher,
        IResearchSource fallback,
        IResearchSource? primary = null)
    {
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _urlFetcher = urlFetcher ?? throw new ArgumentNullException(nameof(urlFetcher));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _primary = primary;
    }

    /// <summary>Research a single destination.</summary>
    public async Task<IReadOnlyList<Suggestion>> ResearchDestinationAsync(
        string destination,
        IReadOnlyList<string> interests,
        int days,
        IReadOnlyList<string>? preferredSources = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(interests);

        var customResults = await FetchCustomSourcesAsync(preferredSources, cancellationToken)
            .ConfigureAwait(false);

        var rawResults = new List<RawResult>();
        if (_primary is not null)
        {
            rawResults.AddRange(
                await _primary.SearchAsync(destination, interests, days, cancellationToken)
                    .ConfigureAwait(false));
        }

        if (rawResults.Count == 0)
        {
            rawResults.AddRange(
                await _fallback.SearchAsync(destination, interests, days, cancellationToken)
                    .ConfigureAwait(false));
        }

        if (rawResults.Count > 0)
        {
            rawResults = await EnrichWithContentAsync(rawResults, cancellationToken).ConfigureAwait(false);
        }

        rawResults.InsertRange(0, customResults);

        return await ShapeAsync(rawResults, interests, days, destination, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<RawResult>> FetchCustomSourcesAsync(
        IReadOnlyList<string>? preferredSources,
        CancellationToken cancellationToken)
    {
        var results = new List<RawResult>();
        if (preferredSources is null)
        {
            return results;
        }

        foreach (var url in preferredSources)
        {
            try
            {
                var content = await _urlFetcher.FetchAsync(url, cancellationToken).ConfigureAwait(false);
                var custom = BuildCustomResult(url, content);
                if (custom is not null)
                {
                    results.Add(custom);
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Malformed or unreachable custom sources are skipped, as in the prototype.
            }
        }

        return results;
    }

    private async Task<List<RawResult>> EnrichWithContentAsync(
        List<RawResult> results,
        CancellationToken cancellationToken)
    {
        var enriched = new RawResult[results.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, results.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                var raw = results[index];
                var current = raw;
                if (!string.IsNullOrWhiteSpace(raw.Url))
                {
                    var content = await _urlFetcher.FetchAsync(raw.Url, token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        current = raw with { Content = Truncate(content, MaxContentLength) };
                    }
                }

                enriched[index] = current;
            }).ConfigureAwait(false);

        return [.. enriched];
    }

    private async Task<IReadOnlyList<Suggestion>> ShapeAsync(
        List<RawResult> rawResults,
        IReadOnlyList<string> interests,
        int days,
        string destination,
        CancellationToken cancellationToken)
    {
        var targetMin = Math.Min(4 * days, 8);
        var targetMax = Math.Min(6 * days, 12);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<RawResult>();
        foreach (var raw in rawResults)
        {
            if (string.IsNullOrWhiteSpace(raw.Title) || !seen.Add(raw.Title))
            {
                continue;
            }

            unique.Add(raw);
            if (unique.Count >= targetMax)
            {
                break;
            }
        }

        var enrichments = new SuggestionEnrichment[unique.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, unique.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                enrichments[index] = await _enricher
                    .EnrichAsync(unique[index], interests, destination, token)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

        var shaped = new List<(RawResult Raw, SuggestionEnrichment Enrichment)>(unique.Count);
        for (var index = 0; index < unique.Count; index++)
        {
            shaped.Add((unique[index], enrichments[index]));
        }

        var downgrade = shaped.Count < targetMin;
        var suggestions = new List<Suggestion>(shaped.Count);
        foreach (var (raw, enrichment) in shaped)
        {
            var (confidence, couldntVerify) = ConfidenceCalculator.Calculate(enrichment, raw.Url);
            if (downgrade && confidence == ConfidenceLevel.High)
            {
                confidence = ConfidenceLevel.Medium;
            }

            suggestions.Add(new Suggestion(
                name: raw.Title,
                destination: destination,
                rationale: enrichment.Rationale,
                area: enrichment.Area,
                typicalDuration: enrichment.TypicalDuration,
                openingHours: enrichment.OpeningHours,
                approximateCost: enrichment.ApproximateCost,
                seasonWeatherFit: enrichment.SeasonWeatherFit,
                sourceLink: string.IsNullOrWhiteSpace(raw.Url) ? null : raw.Url,
                sourceName: raw.SourceName,
                confidence: confidence,
                couldntVerify: couldntVerify));
        }

        return suggestions;
    }

    private static RawResult? BuildCustomResult(string url, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        var lines = content.Split('\n');

        var title = string.Empty;
        foreach (var line in lines.Take(10))
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                title = line[2..].Trim();
                break;
            }
        }

        if (title.Length == 0)
        {
            title = lines.Length > 0 ? Truncate(lines[0], 100) : "Custom source";
        }

        var snippet = lines.Length > 1 ? Truncate(lines[1], 200) : string.Empty;

        return new RawResult(title, url, snippet, ResearchSourceName.Custom, content);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
