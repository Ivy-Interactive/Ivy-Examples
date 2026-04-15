namespace SliplaneDeploy.Services;

using System.Net;
using System.Text.RegularExpressions;
using SliplaneDeploy.Models;

/// <summary>
/// Result of resolving which Dockerfile path, Docker context, and extra env vars to send to Sliplane.
/// </summary>
public record DockerfileResolution(
    string DockerfilePath,
    string DockerContext,
    IReadOnlyList<EnvironmentVariable>? AdditionalEnv = null);

/// <summary>
/// When the app folder has no Dockerfile, falls back to <c>.github/docker/Dockerfile.ivy-default</c>.
/// For monorepo subfolders the context is switched to repo root (<c>"."</c>) so the shared Dockerfile
/// is inside the build context, and <c>IVY_APP_DIR</c> is passed as an env/build-arg so the Dockerfile
/// knows which subfolder to build.
/// </summary>
public class GitHubDockerfilePathResolver
{
    private static readonly Regex GitHubRepoRegex = new(
        @"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?/?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GitHubDockerfilePathResolver(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public static bool TryParseGitHubRepo(string? gitRepoUrl, out string owner, out string repo)
    {
        owner = repo = "";
        if (string.IsNullOrWhiteSpace(gitRepoUrl)) return false;
        var trimmed = gitRepoUrl.Trim().TrimEnd('/');
        var m = GitHubRepoRegex.Match(trimmed);
        if (!m.Success) return false;
        owner = m.Groups["owner"].Value;
        repo = m.Groups["repo"].Value;
        return true;
    }

    /// <summary>
    /// Resolves Dockerfile path, Docker context and optional extra env vars for creating a Sliplane service.
    /// </summary>
    public async Task<DockerfileResolution> ResolveAsync(
        string? gitRepoUrl,
        string branch,
        string dockerfilePath,
        string dockerContext,
        CancellationToken cancellationToken = default)
    {
        var contextTrim = string.IsNullOrWhiteSpace(dockerContext) ? "." : dockerContext.Trim();
        var path = string.IsNullOrWhiteSpace(dockerfilePath) ? "Dockerfile" : dockerfilePath.Trim();
        if (!TryParseGitHubRepo(gitRepoUrl, out var owner, out var repo))
            return new DockerfileResolution(path, contextTrim);

        var branchTrim = string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim();

        // 1. If the requested Dockerfile exists on GitHub, use it as-is.
        if (await ExistsOnGitHubRawAsync(owner, repo, branchTrim, path, cancellationToken).ConfigureAwait(false))
            return new DockerfileResolution(path, contextTrim);

        // 2. Try the shared default Dockerfile.
        var fallback = (_configuration["Sliplane:DefaultDockerfilePath"] ?? ".github/docker/Dockerfile.ivy-default").Trim();
        if (string.IsNullOrEmpty(fallback) || string.Equals(fallback, path, StringComparison.Ordinal))
            return new DockerfileResolution(path, contextTrim);

        if (!await ExistsOnGitHubRawAsync(owner, repo, branchTrim, fallback, cancellationToken).ConfigureAwait(false))
            return new DockerfileResolution(path, contextTrim);

        // 3. Shared Dockerfile exists. Switch context to repo root so the Dockerfile is inside the
        //    build context (avoids Sliplane rewriting to ../../… which uploads as 2B).
        //    Pass IVY_APP_DIR so the Dockerfile knows which subfolder contains the *.csproj.
        var appDir = NormalizeRepoRelativePath(contextTrim);
        List<EnvironmentVariable>? extraEnv = null;
        if (appDir.Length > 0 && appDir != ".")
            extraEnv = [new EnvironmentVariable("IVY_APP_DIR", appDir, Secret: false)];

        return new DockerfileResolution(
            DockerfilePath: fallback,
            DockerContext: ".",
            AdditionalEnv: extraEnv);
    }

    /// <summary>Normalize to forward-slash path without leading ./ or trailing /.</summary>
    private static string NormalizeRepoRelativePath(string dockerContext)
    {
        var t = dockerContext.Replace('\\', '/').Trim().Trim('/');
        while (t.StartsWith("./", StringComparison.Ordinal))
            t = t[2..];
        return string.IsNullOrEmpty(t) ? "." : t;
    }

    private async Task<bool> ExistsOnGitHubRawAsync(string owner, string repo, string branch, string repoRelativePath, CancellationToken cancellationToken)
    {
        var url = BuildRawUrl(owner, repo, branch, repoRelativePath);
        try
        {
            using var client = _httpClientFactory.CreateClient("GitHubRaw");
            using (var head = new HttpRequestMessage(HttpMethod.Head, url))
            {
                using var headResponse = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (headResponse.StatusCode == HttpStatusCode.OK) return true;
                if (headResponse.StatusCode == HttpStatusCode.NotFound) return false;
            }

            using (var get = new HttpRequestMessage(HttpMethod.Get, url))
            {
                using var getResponse = await client.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (getResponse.StatusCode == HttpStatusCode.OK) return true;
                if (getResponse.StatusCode == HttpStatusCode.NotFound) return false;
            }

            // Rate limits, private repo without token, etc. — do not treat as "missing".
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static string BuildRawUrl(string owner, string repo, string branch, string repoRelativePath)
    {
        var normalized = repoRelativePath.Replace('\\', '/').TrimStart('/');
        var encodedPath = string.Join("/", normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        return $"https://raw.githubusercontent.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/{Uri.EscapeDataString(branch)}/{encodedPath}";
    }
}
