using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TravelMinion.Domain;
using TravelMinion.Infrastructure;

namespace TravelMinion.Tests.Infrastructure;

public class TavilyResearchSourceTests
{
    private static TavilyResearchSource Create(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new TavilyOptions { Endpoint = "https://api.test/v1/search" }));

    private static StubHttpMessageHandler FirstResponseThenEmpty(string json)
    {
        var first = true;
        return new StubHttpMessageHandler(_ =>
        {
            var payload = first ? json : """{"results":[]}""";
            first = false;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
            };
        });
    }

    [Fact]
    public void BuildQueries_uses_top_three_interests_plus_general()
    {
        var queries = TavilyResearchSource.BuildQueries(
            "Tokyo",
            ["food", "history", "nature", "art", "shopping"]);

        queries.Should().HaveCount(4);
        queries[0].Should().Be("best food in Tokyo tourist attractions");
        queries[2].Should().Be("best nature in Tokyo tourist attractions");
        queries[3].Should().Be("top tourist attractions Tokyo travel guide");
    }

    [Fact]
    public async Task Search_parses_results_into_raw_results()
    {
        var handler = FirstResponseThenEmpty(
            """{"results":[{"title":"Senso-ji","url":"https://x/1","content":"A temple"}]}""");

        var results = await Create(handler).SearchAsync("Tokyo", ["history"], 3);

        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Senso-ji");
        results[0].Url.Should().Be("https://x/1");
        results[0].Snippet.Should().Be("A temple");
        results[0].SourceName.Should().Be(ResearchSourceName.Tavily);
    }

    [Fact]
    public async Task Search_skips_results_without_a_title()
    {
        var handler = FirstResponseThenEmpty(
            """{"results":[{"url":"https://x/0"},{"title":"Kyoto Imperial Palace","url":"https://x/1"}]}""");

        var results = await Create(handler).SearchAsync("Kyoto", ["history"], 3);

        results.Should().ContainSingle();
        results[0].Title.Should().Be("Kyoto Imperial Palace");
    }

    [Fact]
    public async Task Search_returns_empty_on_error_status()
    {
        var handler = StubHttpMessageHandler.Status(HttpStatusCode.InternalServerError);

        var results = await Create(handler).SearchAsync("Tokyo", ["history"], 3);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_returns_empty_on_malformed_json()
    {
        var handler = StubHttpMessageHandler.Text("not json");

        var results = await Create(handler).SearchAsync("Tokyo", ["history"], 3);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_returns_empty_when_the_request_throws()
    {
        var results = await Create(StubHttpMessageHandler.Throw()).SearchAsync("Tokyo", ["history"], 3);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_returns_empty_when_the_request_times_out()
    {
        var results = await Create(StubHttpMessageHandler.Timeout()).SearchAsync("Tokyo", ["history"], 3);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_posts_the_query_to_the_configured_endpoint()
    {
        var handler = StubHttpMessageHandler.Json("""{"results":[]}""");

        await Create(handler).SearchAsync("Tokyo", ["history"], 3);

        handler.Requests.Should().OnlyContain(
            request => request.Method == HttpMethod.Post && request.RequestUri!.ToString() == "https://api.test/v1/search");
        handler.Bodies.Should().Contain(body => body.Contains("history", StringComparison.Ordinal));
    }
}
