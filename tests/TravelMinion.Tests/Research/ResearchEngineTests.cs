using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ResearchEngineTests
{
    private static readonly SuggestionEnrichment FullEnrichment =
        new("Placeholder", "Matches food", "Shibuya", "2 hours", "9am-5pm", "Free", "Year-round");

    private static RawResult Raw(
        string title,
        ResearchSourceName source = ResearchSourceName.Tavily,
        string url = "https://example.com/x")
        => new(title, url, "snippet", source);

    private static ResearchEngine Engine(
        FakeResearchEnricher enricher,
        IUrlFetcher fetcher,
        IResearchSource fallback,
        IResearchSource? primary = null)
        => new(enricher, fetcher, fallback, primary);

    [Fact]
    public async Task Uses_primary_source_when_available()
    {
        var primary = new FakeResearchSource(new[] { Raw("Tokyo Tower") });
        var fallback = new FakeResearchSource(new[] { Raw("Fallback Thing", ResearchSourceName.DuckDuckGo) });
        var engine = Engine(new FakeResearchEnricher(), new FakeUrlFetcher(), fallback, primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        results.Should().ContainSingle().Which.Name.Should().Be("Tokyo Tower");
        fallback.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Falls_back_when_primary_returns_nothing()
    {
        var primary = new FakeResearchSource(Array.Empty<RawResult>());
        var fallback = new FakeResearchSource(new[] { Raw("Fallback Thing", ResearchSourceName.DuckDuckGo) });
        var engine = Engine(new FakeResearchEnricher(), new FakeUrlFetcher(), fallback, primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        results.Should().ContainSingle().Which.Name.Should().Be("Fallback Thing");
        fallback.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Uses_fallback_when_no_primary_is_configured()
    {
        var fallback = new FakeResearchSource(new[] { Raw("Only Fallback", ResearchSourceName.DuckDuckGo) });
        var engine = Engine(new FakeResearchEnricher(), new FakeUrlFetcher(), fallback);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        results.Should().ContainSingle().Which.Name.Should().Be("Only Fallback");
    }

    [Fact]
    public async Task Prepends_custom_sources()
    {
        var fetcher = new FakeUrlFetcher().WithPage("https://guide.test/x", "# My Guide\nIntro line");
        var primary = new FakeResearchSource(new[] { Raw("Tokyo Tower") });
        var engine = Engine(new FakeResearchEnricher(), fetcher, new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var results = await engine.ResearchDestinationAsync(
            "Tokyo", new[] { "food" }, 1, new[] { "https://guide.test/x" });

        results.Should().HaveCount(2);
        results[0].Name.Should().Be("My Guide");
        results[0].SourceName.Should().Be(ResearchSourceName.Custom);
        results[1].Name.Should().Be("Tokyo Tower");
    }

    [Fact]
    public async Task Deduplicates_by_title_case_insensitively()
    {
        var primary = new FakeResearchSource(new[] { Raw("Tokyo Tower"), Raw("tokyo tower"), Raw("Shibuya") });
        var engine = Engine(new FakeResearchEnricher(), new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        results.Should().HaveCount(2);
        results.Select(s => s.Name).Should().Equal("Tokyo Tower", "Shibuya");
    }

    [Fact]
    public async Task Caps_at_target_maximum_for_the_day_count()
    {
        var many = Enumerable.Range(1, 10).Select(i => Raw($"Spot {i}")).ToArray();
        var enricher = new FakeResearchEnricher();
        var engine = Engine(enricher, new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), new FakeResearchSource(many));

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        results.Should().HaveCount(6);
        enricher.CallCount.Should().Be(6);
    }

    [Fact]
    public async Task Downgrades_high_confidence_when_below_target_minimum()
    {
        var primary = new FakeResearchSource(new[] { Raw("Sparse") });
        var engine = Engine(new FakeResearchEnricher(FullEnrichment), new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 3);

        results.Single().Confidence.Should().Be(ConfidenceLevel.Medium);
    }

    [Fact]
    public async Task Keeps_high_confidence_when_target_minimum_is_met()
    {
        var four = Enumerable.Range(1, 4).Select(i => Raw($"Spot {i}")).ToArray();
        var engine = Engine(new FakeResearchEnricher(FullEnrichment), new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), new FakeResearchSource(four));

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        results.Should().HaveCount(4);
        results.Should().OnlyContain(s => s.Confidence == ConfidenceLevel.High);
    }

    [Fact]
    public async Task Populates_suggestion_fields_from_enrichment()
    {
        var enrichment = new SuggestionEnrichment("Ramen Tour", "Matches food", "Shibuya", "3 hours", "10am-6pm", "1000 yen", "Best in spring");
        var primary = new FakeResearchSource(new[] { Raw("Ramen Tour", ResearchSourceName.Custom, "https://ex/ramen") });
        var engine = Engine(new FakeResearchEnricher(enrichment), new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var suggestion = (await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1)).Single();

        suggestion.Rationale.Should().Be("Matches food");
        suggestion.Area.Should().Be("Shibuya");
        suggestion.TypicalDuration.Should().Be("3 hours");
        suggestion.OpeningHours.Should().Be("10am-6pm");
        suggestion.ApproximateCost.Should().Be("1000 yen");
        suggestion.SeasonWeatherFit.Should().Be("Best in spring");
        suggestion.SourceLink.Should().Be("https://ex/ramen");
        suggestion.SourceName.Should().Be(ResearchSourceName.Custom);
        suggestion.Destination.Should().Be("Tokyo");
    }

    [Fact]
    public async Task Enriches_hits_with_fetched_page_content()
    {
        var enricher = new FakeResearchEnricher();
        var fetcher = new FakeUrlFetcher().WithPage("https://ex/1", "FULL PAGE CONTENT");
        var primary = new FakeResearchSource(new[] { new RawResult("Spot", "https://ex/1", "snip", ResearchSourceName.Tavily) });
        var engine = Engine(enricher, fetcher, new FakeResearchSource(Array.Empty<RawResult>()), primary);

        await engine.ResearchDestinationAsync("Tokyo", new[] { "food" }, 1);

        enricher.Received.Should().ContainSingle();
        enricher.Received[0].Content.Should().Be("FULL PAGE CONTENT");
    }

    [Fact]
    public async Task Expands_a_list_page_into_one_suggestion_per_activity()
    {
        var enricher = new FakeResearchEnricher(_ => new[]
        {
            new SuggestionEnrichment("Senso-ji", "Temple", "Asakusa", "2 hours"),
            new SuggestionEnrichment("Tokyo Skytree", "Views", "Sumida", "1-2 hours"),
        });
        var primary = new FakeResearchSource(new[] { Raw("Top 10 Tokyo Attractions") });
        var engine = Engine(enricher, new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "history" }, 1);

        results.Select(s => s.Name).Should().Equal("Senso-ji", "Tokyo Skytree");
        results.Should().OnlyContain(s => s.SourceLink == "https://example.com/x");
    }

    [Fact]
    public async Task Deduplicates_activities_by_name_across_pages()
    {
        var enricher = new FakeResearchEnricher(_ => new[]
        {
            new SuggestionEnrichment("Senso-ji", "Temple", "Asakusa", "2 hours"),
        });
        var primary = new FakeResearchSource(new[] { Raw("Guide A"), Raw("Guide B") });
        var engine = Engine(enricher, new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "history" }, 1);

        results.Should().ContainSingle().Which.Name.Should().Be("Senso-ji");
    }

    [Fact]
    public async Task Drops_pages_that_yield_no_activities()
    {
        var enricher = new FakeResearchEnricher(_ => Array.Empty<SuggestionEnrichment>());
        var primary = new FakeResearchSource(new[] { Raw("Best time to visit Tokyo") });
        var engine = Engine(enricher, new FakeUrlFetcher(), new FakeResearchSource(Array.Empty<RawResult>()), primary);

        var results = await engine.ResearchDestinationAsync("Tokyo", new[] { "history" }, 1);

        results.Should().BeEmpty();
    }

}
