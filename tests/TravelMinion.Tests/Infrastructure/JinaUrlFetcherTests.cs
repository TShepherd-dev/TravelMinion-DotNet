using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TravelMinion.Infrastructure;

namespace TravelMinion.Tests.Infrastructure;

public class JinaUrlFetcherTests
{
    private static JinaUrlFetcher Create(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new JinaOptions { BaseUrl = "https://r.jina.ai/" }));

    [Fact]
    public async Task Fetch_returns_page_content_on_success()
    {
        var handler = StubHttpMessageHandler.Text("# Senso-ji\nA temple in Asakusa.");

        var content = await Create(handler).FetchAsync("https://example.com/sensoji");

        content.Should().Be("# Senso-ji\nA temple in Asakusa.");
    }

    [Fact]
    public async Task Fetch_requests_the_url_through_the_reader_base()
    {
        var handler = StubHttpMessageHandler.Text("ok");

        await Create(handler).FetchAsync("https://example.com/sensoji");

        handler.Requests.Should().ContainSingle()
            .Which.RequestUri!.ToString().Should().Be("https://r.jina.ai/https://example.com/sensoji");
    }

    [Fact]
    public async Task Fetch_returns_null_on_error_status()
    {
        var handler = StubHttpMessageHandler.Status(HttpStatusCode.NotFound);

        var content = await Create(handler).FetchAsync("https://example.com/missing");

        content.Should().BeNull();
    }

    [Fact]
    public async Task Fetch_returns_null_when_the_request_throws()
    {
        var content = await Create(StubHttpMessageHandler.Throw()).FetchAsync("https://example.com/x");

        content.Should().BeNull();
    }

    [Fact]
    public async Task Fetch_returns_null_when_the_request_times_out()
    {
        var content = await Create(StubHttpMessageHandler.Timeout()).FetchAsync("https://example.com/slow");

        content.Should().BeNull();
    }

    [Fact]
    public async Task Fetch_returns_null_for_blank_urls()
    {
        var handler = StubHttpMessageHandler.Text("ok");

        var content = await Create(handler).FetchAsync("   ");

        content.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }
}
