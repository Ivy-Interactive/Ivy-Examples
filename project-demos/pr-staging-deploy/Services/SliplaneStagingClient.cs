namespace PrStagingDeploy.Services;

using System.Net.Http.Headers;
using System.Text;
using PrStagingDeploy.Models;

/// <summary>Sliplane API client for creating/deleting staging services.</summary>
public class SliplaneStagingClient
{
    private const string BaseUrl = "https://ctrl.sliplane.io/v0";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SliplaneStagingClient> _logger;

    public SliplaneStagingClient(IHttpClientFactory httpClientFactory, ILogger<SliplaneStagingClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>Sanitize branch name for use in service/domain (alphanumeric + hyphen).</summary>
    public static string SanitizeBranchName(string branch)
    {
        var safe = new string(branch
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());
        return string.IsNullOrEmpty(safe) ? "branch" : safe;
    }

    /// <summary>Result of CreateServiceAsync: (service info or null, error message if failed).</summary>
    public record CreateServiceResult(SliplaneServiceInfo? Service, string? Error);

    public async Task<CreateServiceResult> CreateServiceAsync(
        string apiToken,
        string projectId,
        string serverId,
        string name,
        string gitRepo,
        string branch,
        string dockerfilePath,
        string dockerContext)
    {
        var client = CreateClient(apiToken);
        var body = new
        {
            name,
            serverId,
            network = new { @public = true, protocol = "http" },
            deployment = new
            {
                url = gitRepo,
                branch,
                autoDeploy = true,
                dockerfilePath,
                dockerContext
            },
            healthcheck = "/"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"{BaseUrl}/projects/{projectId}/services", content);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Sliplane create service failed {StatusCode} for {Name}: {Response}", response.StatusCode, name, responseBody);
            string? errMsg = null;
            try
            {
                using var errDoc = JsonDocument.Parse(responseBody);
                if (errDoc.RootElement.TryGetProperty("message", out var m))
                    errMsg = m.GetString();
            }
            catch { }
            return new CreateServiceResult(null, errMsg ?? responseBody);
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = root.GetProperty("id").GetString();
        var managedDomain = root.TryGetProperty("network", out var net) && net.TryGetProperty("managedDomain", out var md)
            ? md.GetString()
            : null;
        return new CreateServiceResult(new SliplaneServiceInfo(id ?? "", managedDomain ?? ""), null);
    }

    public async Task<bool> DeleteServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var client = CreateClient(apiToken);
        var response = await client.DeleteAsync($"{BaseUrl}/projects/{projectId}/services/{serviceId}");
        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

    public async Task<bool> RedeployServiceAsync(string apiToken, string projectId, string serviceId)
    {
        var client = CreateClient(apiToken);
        var response = await client.PostAsync($"{BaseUrl}/projects/{projectId}/services/{serviceId}/deploy", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<SliplaneServiceInfo>> ListStagingServicesAsync(string apiToken, string projectId, string prefix)
    {
        var client = CreateClient(apiToken);
        var response = await client.GetAsync($"{BaseUrl}/projects/{projectId}/services");
        if (!response.IsSuccessStatusCode)
            return new List<SliplaneServiceInfo>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        JsonElement servicesEl = doc.RootElement.ValueKind == JsonValueKind.Array
            ? doc.RootElement
            : (doc.RootElement.TryGetProperty("services", out var s) ? s : doc.RootElement);
        var list = new List<SliplaneServiceInfo>();
        foreach (var el in servicesEl.EnumerateArray())
        {
            var name = el.GetProperty("name").GetString() ?? "";
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var id = el.GetProperty("id").GetString() ?? "";
            var managedDomain = el.TryGetProperty("network", out var net) && net.TryGetProperty("managedDomain", out var md)
                ? md.GetString()
                : null;
            var createdAt = el.TryGetProperty("createdAt", out var ca)
                ? DateTime.Parse(ca.GetString() ?? "1970-01-01")
                : DateTime.MinValue;
            var status = el.TryGetProperty("status", out var st) ? st.GetString() : null;
            list.Add(new SliplaneServiceInfo(id, managedDomain ?? "", name, createdAt, status));
        }
        return list;
    }

    public async Task<List<SliplaneServiceEvent>> GetServiceEventsAsync(string apiToken, string projectId, string serviceId)
    {
        var client = CreateClient(apiToken);
        var response = await client.GetAsync($"{BaseUrl}/projects/{projectId}/services/{serviceId}/events");
        if (!response.IsSuccessStatusCode) return new List<SliplaneServiceEvent>();

        var json = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var arr = root.ValueKind == JsonValueKind.Array ? root : (root.TryGetProperty("events", out var e) ? e : root);
            if (arr.ValueKind != JsonValueKind.Array) return new List<SliplaneServiceEvent>();

            var list = new List<SliplaneServiceEvent>();
            foreach (var el in arr.EnumerateArray())
            {
                var type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                var msg = el.TryGetProperty("message", out var m) ? m.GetString() : null;
                var triggeredBy = el.TryGetProperty("triggeredBy", out var tb) ? tb.GetString() : null;
                var reason = el.TryGetProperty("reason", out var r) ? r.GetString() : null;
                var createdAt = el.TryGetProperty("createdAt", out var ca) ? DateTime.Parse(ca.GetString() ?? "1970-01-01") : DateTime.MinValue;
                list.Add(new SliplaneServiceEvent(type, msg, createdAt, triggeredBy, reason));
            }
            return list;
        }
        catch { return new List<SliplaneServiceEvent>(); }
    }

    private HttpClient CreateClient(string apiToken)
    {
        var client = _httpClientFactory.CreateClient("Sliplane");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        return client;
    }
}

public record SliplaneServiceInfo(string Id, string ManagedDomain, string? Name = null, DateTime CreatedAt = default, string? Status = null);

