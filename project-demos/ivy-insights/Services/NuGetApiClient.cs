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

    public async Task<NuGetPackageIndex> GetPackageIndexAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var index = await response.Content.ReadFromJsonAsync<NuGetPackageIndex>(_jsonOptions, cancellationToken);
        if (index == null)
        {
            throw new Exception($"Failed to parse package index for {packageId}");
        }
        
        return index;
    }

    public async Task<NuGetRegistrationIndex> GetPackageRegistrationAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.nuget.org/v3/registration5-semver1/{packageId.ToLowerInvariant()}/index.json";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var registration = await response.Content.ReadFromJsonAsync<NuGetRegistrationIndex>(_jsonOptions, cancellationToken);
        if (registration == null)
        {
            throw new Exception($"Failed to parse package registration for {packageId}");
        }
        
        return registration;
    }

    public async Task<List<VersionInfo>> GetAllVersionsFromRegistrationAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var registration = await GetPackageRegistrationAsync(packageId, cancellationToken);
        var allVersions = new List<VersionInfo>();

        // Registration API returns pages, need to fetch all pages recursively
        foreach (var page in registration.Items)
        {
            await FetchPageVersionsAsync(page, allVersions, cancellationToken);
        }

        // Deduplicate versions and sort by published date
        return allVersions
            .GroupBy(v => v.Version)
            .Select(g => g.First())
            .OrderByDescending(v => v.Published ?? DateTime.MinValue)
            .ToList();
    }

    private async Task FetchPageVersionsAsync(NuGetRegistrationPage page, List<VersionInfo> allVersions, CancellationToken cancellationToken)
    {
        // If page has direct items, use them
        if (page.Items != null && page.Items.Count > 0)
        {
            foreach (var item in page.Items)
            {
                if (item.CatalogEntry != null)
                {
                    // CatalogEntry.Published is the authoritative source for publish date
                    // Handle both DateTime and DateTimeOffset
                    DateTime? publishedDate = null;
                    if (item.CatalogEntry.Published.HasValue)
                    {
                        publishedDate = item.CatalogEntry.Published.Value.Kind == DateTimeKind.Utc
                            ? item.CatalogEntry.Published.Value
                            : item.CatalogEntry.Published.Value.ToUniversalTime();
                    }

                    allVersions.Add(new VersionInfo
                    {
                        Version = item.CatalogEntry.Version,
                        Published = publishedDate,
                        Downloads = item.CatalogEntry.TotalDownloads
                    });
                }
            }
            return; // We got items, no need to fetch page URL
        }

        // If page doesn't have items, it's a link to another page - fetch it
        if (!string.IsNullOrEmpty(page.Id))
        {
            try
            {
                var pageResponse = await _httpClient.GetAsync(page.Id, cancellationToken);
                if (pageResponse.IsSuccessStatusCode)
                {
                    var pageData = await pageResponse.Content.ReadFromJsonAsync<NuGetRegistrationPage>(_jsonOptions, cancellationToken);
                    if (pageData != null)
                    {
                        // Recursively process this page (it might have items or more nested pages)
                        await FetchPageVersionsAsync(pageData, allVersions, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Error fetching registration page {page.Id}: {ex.Message}");
            }
        }
    }

    public async Task<NuGetSearchResult?> GetPackageMetadataAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchUrl = $"https://api.nuget.org/v3/query?q={Uri.EscapeDataString(packageId)}&take=1";
            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var searchData = await response.Content.ReadFromJsonAsync<NuGetSearchResponse>(_jsonOptions, cancellationToken);
            return searchData?.Data?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

}

