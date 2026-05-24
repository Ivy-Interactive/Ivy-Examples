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
        var normalizedPackageId = packageId.ToLowerInvariant();
        var url = $"https://azuresearch-usnc.nuget.org/query?q=packageid:{Uri.EscapeDataString(normalizedPackageId)}&take=1000&prerelease=true&semVerLevel=2.0.0";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var data = await response.Content.ReadFromJsonAsync<NuGetSearchResponse>(_jsonOptions, cancellationToken);
            return data?.Data?.FirstOrDefault(x =>
                x.Id != null && x.Id.Equals(normalizedPackageId, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    public async Task<Dictionary<string, long?>> GetAdditionalVersionDownloadsAsync(string packageId, List<string> versions, CancellationToken cancellationToken = default)
    {
        var normalizedPackageId = packageId.ToLowerInvariant();
        var result = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        var url = $"https://api.nuget.org/v3/query?q={Uri.EscapeDataString(normalizedPackageId)}&take=1000&prerelease=true&semVerLevel=2.0.0";
        var normalizedVersions = versions.Select(NormalizeVersion).ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return result;

            var data = await response.Content.ReadFromJsonAsync<NuGetSearchResponse>(_jsonOptions, cancellationToken);
            var package = data?.Data?.FirstOrDefault(x =>
                x.Id != null && x.Id.Equals(normalizedPackageId, StringComparison.OrdinalIgnoreCase));

            if (package?.Versions != null)
            {
                foreach (var searchVersion in package.Versions)
                {
                    var normalizedKey = NormalizeVersion(searchVersion.Version);
                    if (normalizedVersions.Contains(normalizedKey) && !result.ContainsKey(normalizedKey))
                    {
                        result[normalizedKey] = searchVersion.Downloads;
                    }
                }
            }
        }
        catch
        {
            return result;
        }

        return result;
    }

    public static string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        var baseVersion = version.Split('+')[0].Trim().ToLowerInvariant();
        var parts = baseVersion.Split('-');
        var versionPart = parts[0];
        var prereleasePart = parts.Length > 1 ? string.Join("-", parts.Skip(1)) : null;

        if (System.Version.TryParse(versionPart, out var v))
        {
            var segments = new List<string> { v.Major.ToString(), v.Minor.ToString() };

            if (v.Build >= 0)
            {
                segments.Add(v.Build.ToString());
                if (v.Revision >= 0)
                {
                    segments.Add(v.Revision.ToString());
                }
            }

            var normalized = string.Join(".", segments);
            return !string.IsNullOrEmpty(prereleasePart) ? $"{normalized}-{prereleasePart}" : normalized;
        }

        return baseVersion;
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
                catch
                {
                }
            }
        }

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
            catch
            {
            }
        }
    }
}
