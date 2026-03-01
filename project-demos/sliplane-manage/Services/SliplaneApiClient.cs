namespace SliplaneManage.Services;

using SliplaneManage.Models;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// HTTP client for the Sliplane REST API (https://ctrl.sliplane.io/v0)
/// </summary>
public class SliplaneApiClient
{
    private const string BaseUrl = "https://ctrl.sliplane.io/v0";

    private readonly IHttpClientFactory _httpClientFactory;

    public SliplaneApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // ─── Projects ────────────────────────────────────────────────────────────

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

    public async Task<bool> UpdateProjectAsync(string apiToken, string projectId, string name)
    {
        var body = new { name };
        var response = await SendAsync(HttpMethod.Patch, $"/projects/{projectId}", apiToken, body);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> DeleteProjectAsync(string apiToken, string projectId)
    {
        var response = await SendAsync(HttpMethod.Delete, $"/projects/{projectId}", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    // ─── Servers ─────────────────────────────────────────────────────────────

    public async Task<List<SliplaneServer>> GetServersAsync(string apiToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/servers", apiToken);
        return await ParseArrayAsync<SliplaneServer>(response, "servers") ?? [];
    }

    public async Task<SliplaneServer?> GetServerAsync(string apiToken, string serverId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/servers/{serverId}", apiToken);
        return await ParseObjectAsync<SliplaneServer>(response);
    }

    /// <summary>
    /// GET /servers/{serverId}/metrics?range=1h — API returns an array; we return the first item.
    /// </summary>
    public async Task<SliplaneServerMetrics?> GetServerMetricsAsync(string apiToken, string serverId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/servers/{serverId}/metrics?range=1h", apiToken);
        if (response == null || !response.IsSuccessStatusCode) return null;
        try
        {
            var list = await ParseArrayAsync<SliplaneServerMetrics>(response);
            return list is { Count: > 0 } ? list[0] : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<SliplaneVolume>> GetServerVolumesAsync(string apiToken, string serverId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/servers/{serverId}/volumes", apiToken);
        return await ParseArrayAsync<SliplaneVolume>(response, "volumes") ?? [];
    }

    public async Task<bool> DeleteServerAsync(string apiToken, string serverId)
    {
        var response = await SendAsync(HttpMethod.Delete, $"/servers/{serverId}", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> RebootServerAsync(string apiToken, string serverId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/servers/{serverId}", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    // ─── Services ────────────────────────────────────────────────────────────

    public async Task<List<SliplaneService>> GetServicesAsync(string apiToken, string projectId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services", apiToken);
        return await ParseArrayAsync<SliplaneService>(response, "services") ?? [];
    }

    public async Task<SliplaneService?> GetServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services/{serviceId}", apiToken);
        return await ParseObjectAsync<SliplaneService>(response);
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

    public async Task<bool> UpdateServiceAsync(string apiToken, string projectId, string serviceId, UpdateServiceRequest request)
    {
        var response = await SendAsync(HttpMethod.Patch, $"/projects/{projectId}/services/{serviceId}", apiToken, request);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> DeleteServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Delete, $"/projects/{projectId}/services/{serviceId}", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> PauseServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/projects/{projectId}/services/{serviceId}/pause", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> UnpauseServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/projects/{projectId}/services/{serviceId}/unpause", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> DeployServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/projects/{projectId}/services/{serviceId}/deploy", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> AddDomainAsync(string apiToken, string projectId, string serviceId, string domain)
    {
        var body = new { domain };
        var response = await SendAsync(HttpMethod.Post, $"/projects/{projectId}/services/{serviceId}/domains", apiToken, body);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> DeleteDomainAsync(string apiToken, string projectId, string serviceId, string domainId)
    {
        var response = await SendAsync(HttpMethod.Delete, $"/projects/{projectId}/services/{serviceId}/domains/{domainId}", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<List<SliplaneServiceLog>> GetServiceLogsAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services/{serviceId}/logs", apiToken);
        return await ParseArrayAsync<SliplaneServiceLog>(response) ?? [];
    }

    public async Task<SliplaneServiceMetrics?> GetServiceMetricsAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services/{serviceId}/metrics", apiToken);
        return await ParseObjectAsync<SliplaneServiceMetrics>(response);
    }

    public async Task<List<SliplaneServiceEvent>> GetServiceEventsAsync(string apiToken, string projectId, string serviceId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/projects/{projectId}/services/{serviceId}/events", apiToken);
        return await ParseArrayAsync<SliplaneServiceEvent>(response, "events") ?? [];
    }

    // ─── Registry Credentials ────────────────────────────────────────────────

    public async Task<List<SliplaneRegistryCredential>> GetRegistryCredentialsAsync(string apiToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/registry-credentials", apiToken);
        return await ParseArrayAsync<SliplaneRegistryCredential>(response, "credentials") ?? [];
    }

    public async Task<SliplaneRegistryCredential?> GetRegistryCredentialAsync(string apiToken, string credentialId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/registry-credentials/{credentialId}", apiToken);
        return await ParseObjectAsync<SliplaneRegistryCredential>(response);
    }

    public async Task<SliplaneRegistryCredential?> CreateRegistryCredentialAsync(string apiToken, CreateRegistryCredentialRequest request)
    {
        var response = await SendAsync(HttpMethod.Post, "/registry-credentials", apiToken, request);
        return await ParseObjectAsync<SliplaneRegistryCredential>(response);
    }

    public async Task<bool> UpdateRegistryCredentialAsync(string apiToken, string credentialId, UpdateRegistryCredentialRequest request)
    {
        var response = await SendAsync(HttpMethod.Patch, $"/registry-credentials/{credentialId}", apiToken, request);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<bool> DeleteRegistryCredentialAsync(string apiToken, string credentialId)
    {
        var response = await SendAsync(HttpMethod.Delete, $"/registry-credentials/{credentialId}", apiToken);
        return response?.IsSuccessStatusCode == true;
    }

    // ─── Dashboard aggregation ────────────────────────────────────────────────

    /// <summary>
    /// Fetches all projects, servers, and services in parallel for the dashboard overview.
    /// </summary>
    public async Task<SliplaneOverview> GetOverviewAsync(string apiToken)
    {
        var projectsTask = GetProjectsAsync(apiToken);
        var serversTask  = GetServersAsync(apiToken);
        await Task.WhenAll(projectsTask, serversTask);

        var projects = await projectsTask;
        var servers  = await serversTask;

        // Fetch services for all projects in parallel
        var serviceTasks = projects.Select(p => GetServicesAsync(apiToken, p.Id).ContinueWith(t => (p.Id, t.Result)));
        var serviceResults = await Task.WhenAll(serviceTasks);
        var servicesByProject = serviceResults.ToDictionary(r => r.Id, r => r.Result);

        return new SliplaneOverview(projects, servers, servicesByProject);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("Sliplane");

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

            // Try to unwrap a named array key if the API wraps the array in an object
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
