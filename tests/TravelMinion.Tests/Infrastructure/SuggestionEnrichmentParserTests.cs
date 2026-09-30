using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;
using TravelMinion.Infrastructure;

namespace TravelMinion.Tests.Infrastructure;

public class SuggestionEnrichmentParserTests
{
    [Fact]
    public void Parse_reads_a_full_json_payload()
    {
        const string json =
            """{"rationale":"Matches your interest in history","area":"Asakusa","typicalDuration":"2-3 hours","openingHours":"9am-5pm","approximateCost":"Free","seasonWeatherFit":"Best in spring"}""";

        var enrichment = SuggestionEnrichmentParser.Parse(json);

        enrichment.Rationale.Should().Be("Matches your interest in history");
        enrichment.Area.Should().Be("Asakusa");
        enrichment.TypicalDuration.Should().Be("2-3 hours");
        enrichment.OpeningHours.Should().Be("9am-5pm");
        enrichment.ApproximateCost.Should().Be("Free");
        enrichment.SeasonWeatherFit.Should().Be("Best in spring");
    }

    [Fact]
    public void Parse_extracts_json_from_surrounding_prose_and_fences()
    {
        const string content =
            "Sure! Here is the result:\n```json\n{\"rationale\":\"Popular destination attraction\",\"area\":\"Gion\",\"typicalDuration\":\"1-2 hours\"}\n```";

        var enrichment = SuggestionEnrichmentParser.Parse(content);

        enrichment.Area.Should().Be("Gion");
        enrichment.TypicalDuration.Should().Be("1-2 hours");
    }

    [Fact]
    public void Parse_fills_defaults_for_missing_core_fields()
    {
        const string json = """{"openingHours":"24 hours"}""";

        var enrichment = SuggestionEnrichmentParser.Parse(json);

        enrichment.Rationale.Should().Be(SuggestionEnrichmentParser.DefaultRationale);
        enrichment.Area.Should().Be(SuggestionEnrichmentParser.DefaultArea);
        enrichment.TypicalDuration.Should().Be(SuggestionEnrichmentParser.DefaultDuration);
        enrichment.OpeningHours.Should().Be("24 hours");
        enrichment.ApproximateCost.Should().BeNull();
        enrichment.SeasonWeatherFit.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no json here")]
    public void Parse_falls_back_when_there_is_no_json(string? content)
    {
        var enrichment = SuggestionEnrichmentParser.Parse(content);

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
