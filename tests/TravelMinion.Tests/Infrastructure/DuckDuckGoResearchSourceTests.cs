using FluentAssertions;
using TravelMinion.Domain;
using TravelMinion.Infrastructure;

namespace TravelMinion.Tests.Infrastructure;

public class DuckDuckGoResearchSourceTests
{
    private const string Fixture = """
        <div class="result results_links">
          <a rel="nofollow" class="result__a" href="//duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fsensoji&amp;rut=abc">Senso-<b>ji</b> Temple</a>
          <a class="result__snippet" href="//duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fsensoji&amp;rut=abc">An ancient <b>temple</b> in Asakusa.</a>
        </div>
        <div class="result results_links">
          <a rel="nofollow" class="result__a" href="https://direct.example.com/kyoto">Kyoto Imperial Palace</a>
        </div>
        """;

    [Fact]
    public void BuildQueries_uses_top_two_interests_plus_general()
    {
        var queries = DuckDuckGoResearchSource.BuildQueries("Tokyo", ["food", "history", "art"]);

        queries.Should().HaveCount(3);
        queries[0].Should().Be("best food Tokyo tourist attraction");
        queries[2].Should().Be("top attractions Tokyo travel guide");
    }

    [Fact]
    public void ParseResults_extracts_title_url_and_snippet()
    {
        var results = DuckDuckGoResearchSource.ParseResults(Fixture);

        results.Should().HaveCount(2);
        results[0].Title.Should().Be("Senso-ji Temple");
        results[0].Url.Should().Be("https://example.com/sensoji");
        results[0].Snippet.Should().Be("An ancient temple in Asakusa.");
        results[0].SourceName.Should().Be(ResearchSourceName.DuckDuckGo);
        results[1].Title.Should().Be("Kyoto Imperial Palace");
        results[1].Url.Should().Be("https://direct.example.com/kyoto");
        results[1].Snippet.Should().BeNull();
    }

    [Fact]
    public void ParseResults_returns_empty_for_blank_html()
    {
        DuckDuckGoResearchSource.ParseResults("").Should().BeEmpty();
    }
}
