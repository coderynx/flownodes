using System.Net;
using System.Text.RegularExpressions;

namespace Flownodes.IntegrationTests;

/// <summary>
/// Keeps cookies and follows redirects the way a browser on a trustworthy origin does: a <c>Secure</c> cookie, which
/// the test's plain HTTP would lose in a <see cref="CookieContainer"/>, is kept and sent.
/// </summary>
public sealed partial class Browser : IDisposable
{
    private const int MaxRedirects = 20;
    private const string SessionCookieName = "__Host-flownodes";
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _cookies = [];

    public Browser(Uri address)
    {
        _client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        {
            BaseAddress = address
        };
    }

    public List<string> SessionCookieHeaders { get; } = [];
    public string? SessionCookie => _cookies.GetValueOrDefault(SessionCookieName);
    public void ForgetSessionCookie() => _cookies.Remove(SessionCookieName);

    public Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, path), ct);

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await SendOnceAsync(request, ct);

        var redirects = 0;
        while ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
        {
            (++redirects).ShouldBeLessThan(MaxRedirects);
            var target = location.IsAbsoluteUri ? location : new Uri(response.RequestMessage!.RequestUri!, location);
            response = await SendOnceAsync(new HttpRequestMessage(HttpMethod.Get, target), ct);
        }

        return response;
    }

    public async Task<HttpResponseMessage> SubmitSignInPageAsync(
        HttpResponseMessage page,
        string handler,
        IDictionary<string, string> fields,
        CancellationToken ct)
    {
        var html = await page.Content.ReadAsStringAsync(ct);
        var body = new Dictionary<string, string>(fields)
        {
            ["__RequestVerificationToken"] = AntiforgeryToken().Match(html).Groups["value"].Value,
            ["ReturnUrl"] = WebUtility.HtmlDecode(ReturnUrl().Match(html).Groups["value"].Value)
        };

        return await SendAsync(
            new HttpRequestMessage(HttpMethod.Post, $"/signin{handler}") { Content = new FormUrlEncodedContent(body) },
            ct);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        var response = await _client.SendAsync(request, ct);

        if (response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            foreach (var header in headers)
            {
                Remember(header);
            }
        }

        return response;
    }

    private void Remember(string header)
    {
        var pair = header.Split(';')[0].Split('=', 2);
        if (pair[0] == SessionCookieName)
        {
            SessionCookieHeaders.Add(header);
        }

        if (pair[1].Length == 0 || header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase))
        {
            _cookies.Remove(pair[0]);
        }
        else
        {
            _cookies[pair[0]] = pair[1];
        }
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<value>[^"]+)""")]
    private static partial Regex AntiforgeryToken();

    [GeneratedRegex("""name="ReturnUrl" value="(?<value>[^"]*)""")]
    private static partial Regex ReturnUrl();
}
