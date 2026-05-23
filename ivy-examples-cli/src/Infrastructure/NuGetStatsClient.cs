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

    public async Task<JsonDocument> GetAsync(string path)
    {
        var response = await _http.GetAsync(path);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {err}");
        }
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
