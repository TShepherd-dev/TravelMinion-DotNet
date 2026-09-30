using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;
using TravelMinion.Infrastructure;

namespace TravelMinion.Tests.Infrastructure;

public class SuggestionEnrichmentParserTests
{
    private const string FallbackName = "Fallback Page";

    [Fact]
    public void Parse_reads_a_full_json_payload()
    {
        const string json =
            """{"name":"Senso-ji","rationale":"Matches your interest in history","area":"Asakusa","typicalDuration":"2-3 hours","openingHours":"9am-5pm","approximateCost":"Free","seasonWeatherFit":"Best in spring"}""";

        var enrichment = SuggestionEnrichmentParser.Parse(json, FallbackName).Single();

        enrichment.Name.Should().Be("Senso-ji");
        enrichment.Rationale.Should().Be("Matches your interest in history");
        enrichment.Area.Should().Be("Asakusa");
        enrichment.TypicalDuration.Should().Be("2-3 hours");
        enrichment.OpeningHours.Should().Be("9am-5pm");
        enrichment.ApproximateCost.Should().Be("Free");
        enrichment.SeasonWeatherFit.Should().Be("Best in spring");
    }

    [Fact]
    public void Parse_reads_a_json_array_of_activities()
    {
        const string json =
            """[{"name":"Senso-ji","area":"Asakusa"},{"name":"Tokyo Skytree","area":"Sumida"}]""";

        var enrichments = SuggestionEnrichmentParser.Parse(json, FallbackName);

        enrichments.Select(e => e.Name).Should().Equal("Senso-ji", "Tokyo Skytree");
        enrichments.Select(e => e.Area).Should().Equal("Asakusa", "Sumida");
    }

    [Fact]
    public void Parse_extracts_json_from_surrounding_prose_and_fences()
    {
        const string content =
            "Sure! Here is the result:\n```json\n{\"name\":\"Gion\",\"rationale\":\"Popular destination attraction\",\"area\":\"Gion\",\"typicalDuration\":\"1-2 hours\"}\n```";

        var enrichment = SuggestionEnrichmentParser.Parse(content, FallbackName).Single();

        enrichment.Area.Should().Be("Gion");
        enrichment.TypicalDuration.Should().Be("1-2 hours");
    }

    [Fact]
    public void Parse_fills_defaults_for_missing_core_fields()
    {
        const string json = """{"openingHours":"24 hours"}""";

        var enrichment = SuggestionEnrichmentParser.Parse(json, FallbackName).Single();

        enrichment.Name.Should().Be(FallbackName);
        enrichment.Rationale.Should().Be(SuggestionEnrichmentParser.DefaultRationale);
        enrichment.Area.Should().Be(SuggestionEnrichmentParser.DefaultArea);
        enrichment.TypicalDuration.Should().Be(SuggestionEnrichmentParser.DefaultDuration);
        enrichment.OpeningHours.Should().Be("24 hours");
        enrichment.ApproximateCost.Should().BeNull();
        enrichment.SeasonWeatherFit.Should().BeNull();
    }

    [Fact]
    public void Parse_returns_nothing_for_an_empty_array()
    {
        var enrichments = SuggestionEnrichmentParser.Parse("[]", FallbackName);

        enrichments.Should().BeEmpty();
    }

    [Fact]
    public void Parse_caps_the_number_of_activities_per_page()
    {
        var json = "[" + string.Join(',', Enumerable.Range(1, 20).Select(i => $$"""{"name":"Spot {{i}}"}""")) + "]";

        var enrichments = SuggestionEnrichmentParser.Parse(json, FallbackName);

        enrichments.Should().HaveCount(SuggestionEnrichmentParser.MaxPerPage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no json here")]
    public void Parse_falls_back_when_there_is_no_json(string? content)
    {
        var enrichment = SuggestionEnrichmentParser.Parse(content, FallbackName).Single();

        enrichment.Name.Should().Be(FallbackName);
        enrichment.Rationale.Should().Be(SuggestionEnrichmentParser.DefaultRationale);
        enrichment.Area.Should().Be(SuggestionEnrichmentParser.DefaultArea);
        enrichment.TypicalDuration.Should().Be(SuggestionEnrichmentParser.DefaultDuration);
    }

    [Fact]
    public void BuildPrompt_includes_context_and_truncates_content()
    {
        var raw = new RawResult(
            "Senso-ji",
            "https://example.com",
            "An ancient temple",
            ResearchSourceName.Tavily,
            new string('x', 10_000));

        var prompt = SemanticKernelResearchEnricher.BuildPrompt(raw, ["history", "food"], "Tokyo");

        prompt.Should().Contain("Destination: Tokyo");
        prompt.Should().Contain("Traveller interests: history, food");
        prompt.Should().Contain("Title: Senso-ji");
        prompt.Should().Contain("Snippet: An ancient temple");
        prompt.Should().Contain(new string('x', SemanticKernelResearchEnricher.MaxContentLength));
        prompt.Should().NotContain(new string('x', SemanticKernelResearchEnricher.MaxContentLength + 1));
    }
}
