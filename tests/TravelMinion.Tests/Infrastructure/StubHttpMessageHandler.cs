using System.Net;
using System.Net.Http;

namespace TravelMinion.Tests.Infrastructure;

/// <summary>Test double that records requests and returns canned responses.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    public List<string> Bodies { get; } = new();

    public static StubHttpMessageHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

    public static StubHttpMessageHandler Text(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(text, System.Text.Encoding.UTF8, "text/plain"),
        });

    public static StubHttpMessageHandler Status(HttpStatusCode status) =>
        new(_ => new HttpResponseMessage(status));

    public static StubHttpMessageHandler Throw() =>
        new(_ => throw new HttpRequestException("boom"));

    /// <summary>Simulates an HttpClient timeout (throws TaskCanceledException without caller cancellation).</summary>
    public static StubHttpMessageHandler Timeout() =>
        new(_ => throw new TaskCanceledException("The request timed out.", new TimeoutException()));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content is not null)
        {
            Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return _responder(request);
    }
}
