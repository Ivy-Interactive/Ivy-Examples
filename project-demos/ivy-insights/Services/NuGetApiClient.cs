using IvyInsights.Models;

namespace IvyInsights.Services;

public class NuGetApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public NuGetApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<VersionInfo>> GetAllVersionsFromRegistrationAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var registration = await GetPackageRegistrationAsync(packageId, cancellationToken);
        var allVersions = new List<VersionInfo>();

        foreach (var page in registration.Items)
        {
            await FetchPageVersionsAsync(page, allVersions, cancellationToken);
        }

        return allVersions
            .GroupBy(v => v.Version)
            .Select(g => g.First())
            .OrderByDescending(v => v.Published ?? DateTime.MinValue)
            .ToList();
    }

    public async Task<NuGetSearchResult?> GetPackageMetadataAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.nuget.org/v3/query?q=packageid:{Uri.EscapeDataString(packageId)}&take=1&includePrerelease=true&semVerLevel=2.0.0";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var data = await response.Content.ReadFromJsonAsync<NuGetSearchResponse>(_jsonOptions, cancellationToken);
            return data?.Data?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private async Task<NuGetRegistrationIndex> GetPackageRegistrationAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.nuget.org/v3/registration5-gz-semver2/{packageId.ToLowerInvariant()}/index.json";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var registration = await response.Content.ReadFromJsonAsync<NuGetRegistrationIndex>(_jsonOptions, cancellationToken);
        return registration ?? throw new Exception($"Failed to parse package registration for {packageId}");
    }

    private async Task FetchPageVersionsAsync(NuGetRegistrationPage page, List<VersionInfo> allVersions, CancellationToken cancellationToken)
    {
        // Direct versions: page has items with CatalogEntry
        if (page.Items?.Count > 0 && page.Items[0].CatalogEntry != null)
        {
            foreach (var item in page.Items)
            {
                if (item.CatalogEntry == null) continue;

                var published = item.CatalogEntry.Published;
                if (published.HasValue && published.Value.Kind != DateTimeKind.Utc)
                    published = published.Value.ToUniversalTime();

                allVersions.Add(new VersionInfo
                {
                    Version = item.CatalogEntry.Version,
                    Published = published,
                    Downloads = null
                });
            }
            return;
        }

        // Nested pages: fetch sub-pages recursively
        if (page.Items != null)
        {
            foreach (var item in page.Items)
            {
                if (string.IsNullOrEmpty(item.Id)) continue;

                try
                {
                    var response = await _httpClient.GetAsync(item.Id, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var subPage = await response.Content.ReadFromJsonAsync<NuGetRegistrationPage>(_jsonOptions, cancellationToken);
                        if (subPage != null)
                            await FetchPageVersionsAsync(subPage, allVersions, cancellationToken);
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching sub-page {item.Id}: {ex.Message}");
                }
            }
        }

        // Fallback: page has @id but no items
        if (!string.IsNullOrEmpty(page.Id) && (page.Items == null || page.Items.Count == 0))
        {
            try
            {
                var response = await _httpClient.GetAsync(page.Id, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var pageData = await response.Content.ReadFromJsonAsync<NuGetRegistrationPage>(_jsonOptions, cancellationToken);
                    if (pageData != null)
                        await FetchPageVersionsAsync(pageData, allVersions, cancellationToken);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching page {page.Id}: {ex.Message}");
            }
        }
    }
}

