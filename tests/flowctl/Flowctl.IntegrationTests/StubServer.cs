using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Flowctl.IntegrationTests;

/// <summary>
/// Stands in for a Flownodes server: answers each request from the first route whose method and path match, and
/// records every request with its body.
/// </summary>
public sealed class StubServer : HttpMessageHandler
{
    private readonly List<(HttpMethod Method, string PathAndQuery, Func<RecordedRequest, HttpResponseMessage> Answer)>
        _routes = [];

    private readonly ConcurrentQueue<RecordedRequest> _requests = new();
    public IReadOnlyList<RecordedRequest> Requests => [.. _requests];

    public StubServer On(HttpMethod method, string pathAndQuery, Func<RecordedRequest, HttpResponseMessage> answer)
    {
        _routes.Add((method, pathAndQuery, answer));

        return this;
    }

    public StubServer On(
        HttpMethod method,
        string pathAndQuery,
        HttpStatusCode status,
        string body = "",
        string type = "application/json") =>
        On(method, pathAndQuery, _ => Response(status, body, type));

    public static HttpResponseMessage Response(HttpStatusCode status, string body, string type = "application/json") =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, type) };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var recorded = new RecordedRequest(
            request.Method,
            request.RequestUri!.PathAndQuery,
            request.Headers.Authorization?.ToString(),
            request.Headers.Accept.ToString(),
            request.Content?.Headers.ContentType?.MediaType,
            request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        _requests.Enqueue(recorded);

        var route = _routes.FirstOrDefault(route =>
            route.Method == request.Method && route.PathAndQuery == recorded.PathAndQuery);

        return route.Answer?.Invoke(recorded) ?? Response(HttpStatusCode.NotFound, """{"title": "stub.no_route"}""");
    }
}

public sealed record RecordedRequest(
    HttpMethod Method,
    string PathAndQuery,
    string? Authorization,
    string Accept,
    string? ContentType,
    string Body);
