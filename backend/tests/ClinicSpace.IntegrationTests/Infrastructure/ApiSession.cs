using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicSpace.IntegrationTests.Infrastructure;

/// <summary>
/// Wraps an HttpClient the way the SPA uses the API: cookies are kept by the client, and the
/// XSRF-TOKEN cookie value is copied into the X-XSRF-TOKEN header on every POST.
/// </summary>
public sealed class ApiSession(HttpClient client) : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private string? _xsrfToken;

    public HttpClient Client => client;

    public string? XsrfToken => _xsrfToken;

    public async Task PrimeCsrfAsync() => Capture(await client.GetAsync("/api/auth/csrf"));

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var response = await client.GetAsync(url);
        Capture(response);
        return response;
    }

    public async Task<T> GetJsonAsync<T>(string url)
    {
        var response = await GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, object? body, bool includeCsrfHeader = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body is null ? null : JsonContent.Create(body, options: Json),
        };
        if (includeCsrfHeader && _xsrfToken is not null)
        {
            request.Headers.Add("X-XSRF-TOKEN", _xsrfToken);
        }

        var response = await client.SendAsync(request);
        Capture(response);
        return response;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public void Dispose() => client.Dispose();

    private void Capture(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return;
        }

        foreach (var cookie in cookies)
        {
            const string prefix = "XSRF-TOKEN=";
            if (cookie.StartsWith(prefix, StringComparison.Ordinal))
            {
                _xsrfToken = Uri.UnescapeDataString(cookie.Split(';')[0][prefix.Length..]);
            }
        }
    }
}
