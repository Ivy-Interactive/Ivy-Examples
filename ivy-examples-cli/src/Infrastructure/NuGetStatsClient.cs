using System.Text.Json;

namespace Ivy.Cli.Infrastructure;

public sealed class NuGetStatsClient
{
    private readonly HttpClient _http;

    public NuGetStatsClient(string baseUrl, string? apiKey = null)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
        if (!string.IsNullOrEmpty(apiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public Task<JsonDocument> GetAsync(string path) => GetAsync(path, query: null);

    public async Task<JsonDocument> GetAsync(string path, IReadOnlyDictionary<string, string?>? query)
    {
        var url = path;
        if (query is { Count: > 0 })
        {
            var qs = string.Join("&",
                query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                    .Select(kv =>
                        $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
            if (qs.Length > 0)
                url += "?" + qs;
        }

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {err}");
        }

        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
