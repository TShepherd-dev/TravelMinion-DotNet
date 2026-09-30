using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// An intermediate search hit before it is shaped into a Suggestion. Holds the
/// data returned by a research source, plus any full page content fetched for
/// it (which the enricher uses to derive hours, cost, and the like).
/// </summary>
public sealed record RawResult(
    string Title,
    string Url,
    string? Snippet,
    ResearchSourceName SourceName,
    string? Content = null);
