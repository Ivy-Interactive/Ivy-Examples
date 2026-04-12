namespace SWAPI.Graph.QL.Apps.Services;

using System.Collections.Concurrent;
using System.Text.Json;
using SWAPI.Graph.QL.Apps.Models;

public class SwapiService
{
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SwapiService(HttpClient http)
    {
        _http = http;
    }


    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default) where T : class
    {
        if (_cache.TryGetValue(url, out var cached)) return (T)cached;
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (result != null) _cache[url] = result;
        return result;
    }


    public async Task<List<T>> GetAllAsync<T>(string resource, CancellationToken ct = default) where T : class
    {
        var cacheKey = $"all:{resource}";
        if (_cache.TryGetValue(cacheKey, out var cached)) return (List<T>)cached;

        var all = new List<T>();
        string? url = $"https://swapi.dev/api/{resource}/";
        while (url != null)
        {
            var page = await GetAsync<SwapiPagedResponse<T>>(url, ct);
            if (page == null) break;
            all.AddRange(page.Results);
            url = page.Next;
        }
        _cache[cacheKey] = all;
        return all;
    }


    public static string ExtractId(string url)
    {
        var parts = url.TrimEnd('/').Split('/');
        return parts[^1];
    }


    public async Task<List<(string Name, string Url)>> ResolveNamesAsync<T>(
        List<string> urls, Func<T, string> nameSelector, CancellationToken ct = default) where T : class
    {
        var results = new List<(string Name, string Url)>();
        foreach (var url in urls)
        {
            var item = await GetAsync<T>(url, ct);
            if (item != null) results.Add((nameSelector(item), url));
        }
        return results;
    }
}
