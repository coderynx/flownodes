using System.Net;
using System.Text.RegularExpressions;

namespace Identity.IntegrationTests;

public sealed partial class Browser : IDisposable
{
    private static readonly Uri Origin = new("http://localhost");
    private readonly HttpClient _client;

    public Browser(HttpMessageHandler server)
    {
        _client = new HttpClient(new CookieHandler(server))
        {
            BaseAddress = Origin
        };
    }

    public Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken) =>
        _client.GetAsync(path, cancellationToken);

    public async Task<HttpResponseMessage> PostFormAsync(
        string page,
        string action,
        IDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        var form = await _client.GetAsync(page, cancellationToken);
        var html = await form.Content.ReadAsStringAsync(cancellationToken);
        var token = AntiforgeryToken().Match(html);

        var body = new Dictionary<string, string>(fields);
        if (token.Success)
        {
            body["__RequestVerificationToken"] = token.Groups["value"].Value;
        }

        return await _client.PostAsync(action, new FormUrlEncodedContent(body), cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<value>[^"]+)""")]
    private static partial Regex AntiforgeryToken();

    private sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly CookieContainer _cookies = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var header = _cookies.GetCookieHeader(Origin);
            if (header.Length > 0)
            {
                request.Headers.TryAddWithoutValidation("Cookie", header);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var setCookie in setCookies)
                {
                    _cookies.SetCookies(Origin, setCookie);
                }
            }

            return response;
        }
    }
}
