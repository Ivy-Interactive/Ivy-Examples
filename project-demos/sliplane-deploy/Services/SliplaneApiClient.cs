namespace SliplaneDeploy.Services;

using SliplaneDeploy.Models;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// HTTP client for the Sliplane REST API (https://ctrl.sliplane.io/v0).
/// Deploy app uses: projects, servers, create service, service events.
/// </summary>
public class SliplaneApiClient
{
    private const string BaseUrl = "https://ctrl.sliplane.io/v0";

    private readonly IHttpClientFactory _httpClientFactory;

    public SliplaneApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<SliplaneProject>> GetProjectsAsync(string apiToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/projects", apiToken);
        return await ParseArrayAsync<SliplaneProject>(response, "projects") ?? [];
    }

    public async Task<SliplaneProject?> CreateProjectAsync(string apiToken, string name)
    {
        var body = new { name };
        var response = await SendAsync(HttpMethod.Post, "/projects", apiToken, body);
        return await ParseObjectAsync<SliplaneProject>(response);
    }

    public async Task<List<SliplaneServer>> GetServersAsync(string apiToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/servers", apiToken);
        return await ParseArrayAsync<SliplaneServer>(response, "servers") ?? [];
    }

    public async Task<SliplaneService?> CreateServiceAsync(string apiToken, string projectId, CreateServiceRequest request)
    {
        var response = await SendAsync(HttpMethod.Post, $"/projects/{projectId}/services", apiToken, request);

        if (response == null)
        {
            throw new InvalidOperationException("Sliplane API did not respond when creating the service. Check your network connection and API token.");
        }

        var responseBody = string.Empty;
        try { responseBody = await response.Content.ReadAsStringAsync(); } catch { }

        if (!response.IsSuccessStatusCode)
        {
            var message = $"Sliplane API returned {(int)response.StatusCode} {response.StatusCode} when creating the service.";
            if (!string.IsNullOrWhiteSpace(responseBody))
                message += $" Details: {responseBody}";

            throw new InvalidOperationException(message);
        }

        return string.IsNullOrWhiteSpace(responseBody)
            ? null
            : JsonSerializer.Deserialize<SliplaneService>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<SliplaneService?> GetServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services/{serviceId}", apiToken);
        return await ParseObjectAsync<SliplaneService>(response);
    }

    public async Task<List<SliplaneServiceEvent>> GetServiceEventsAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services/{serviceId}/events", apiToken);
        return await ParseArrayAsync<SliplaneServiceEvent>(response, "events") ?? [];
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Ivy");

    private async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string path, string apiToken, object? body = null)
    {
        try
        {
            using var client = CreateClient();
            using var request = new HttpRequestMessage(method, $"{BaseUrl}{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    }),
                    Encoding.UTF8,
                    "application/json"
                );
            }

            return await client.SendAsync(request);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<T?> ParseObjectAsync<T>(HttpResponseMessage? response)
    {
        if (response == null || !response.IsSuccessStatusCode) return default;
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return default; }
    }

    private async Task<List<T>?> ParseArrayAsync<T>(HttpResponseMessage? response, string? arrayKey = null)
    {
        if (response == null || !response.IsSuccessStatusCode) return null;
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;

            if (arrayKey != null && root.ValueKind == JsonValueKind.Object && root.TryGetProperty(arrayKey, out var prop))
            {
                root = prop;
            }

            if (root.ValueKind != JsonValueKind.Array) return null;

            return JsonSerializer.Deserialize<List<T>>(root.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }
}
