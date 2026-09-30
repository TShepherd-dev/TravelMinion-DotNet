using TravelMinion.Application;

namespace TravelMinion.Tests;

internal sealed class FakeResearchSource : IResearchSource
{
    private readonly Func<string, IReadOnlyList<RawResult>> _factory;

    public FakeResearchSource(IReadOnlyList<RawResult> results)
        : this(_ => results)
    {
    }

    public FakeResearchSource(Func<string, IReadOnlyList<RawResult>> factory)
        => _factory = factory;

    public int CallCount { get; private set; }

    public List<string> Destinations { get; } = new();

    public Task<IReadOnlyList<RawResult>> SearchAsync(
        string destination,
        IReadOnlyList<string> interests,
        int days,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        Destinations.Add(destination);
        return Task.FromResult(_factory(destination));
    }
}

internal sealed class FakeUrlFetcher : IUrlFetcher
{
    private readonly Dictionary<string, string> _pages = new();

    public List<string> Fetched { get; } = new();

    public FakeUrlFetcher WithPage(string url, string content)
    {
        _pages[url] = content;
        return this;
    }

    public Task<string?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        Fetched.Add(url);
        return Task.FromResult(_pages.TryGetValue(url, out var content) ? content : null);
    }
}

internal sealed class FakeResearchEnricher : IResearchEnricher
{
    private static readonly SuggestionEnrichment DefaultFields =
        new("Default", "Matches your interests", "City-wide", "2 hours", "9am-5pm", "Free");

    private readonly Func<RawResult, IReadOnlyList<SuggestionEnrichment>> _factory;

    public FakeResearchEnricher(SuggestionEnrichment? enrichment = null)
        : this(raw => new[] { (enrichment ?? DefaultFields) with { Name = raw.Title } })
    {
    }

    public FakeResearchEnricher(Func<RawResult, IReadOnlyList<SuggestionEnrichment>> factory)
        => _factory = factory;

    public int CallCount { get; private set; }

    public List<RawResult> Received { get; } = new();

    public Task<IReadOnlyList<SuggestionEnrichment>> EnrichAsync(
        RawResult raw,
        IReadOnlyList<string> interests,
        string destination,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        Received.Add(raw);
        return Task.FromResult(_factory(raw));
    }
}
