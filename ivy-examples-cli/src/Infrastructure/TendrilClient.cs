using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ivy.Cli.Infrastructure;

/// <summary>
/// HTTP client for the tendril-deploy REST API.
/// Base URL is read from TENDRIL_BASE_URL env var or --tendril-url flag.
/// Auth is optional: if TendrilDeploy:ApiKey is configured on the server,
/// pass it via TENDRIL_API_KEY or --tendril-api-key.
/// </summary>
public sealed class TendrilClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly HttpClient _http;

    public TendrilClient(string baseUrl, string? apiKey = null)
    {
        var uri = baseUrl.TrimEnd('/') + "/";
        _http = new HttpClient { BaseAddress = new Uri(uri) };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(apiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<JsonDocument> GetAsync(string path, string? sliplaneToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrEmpty(sliplaneToken))
            request.Headers.Add("X-Sliplane-Token", sliplaneToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccess(response);
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    public async Task<JsonDocument> PostAsync(string path, object body)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(path, content);
        await EnsureSuccess(response);
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");
        }
    }
}
