namespace SliplaneDeploy.Services;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Parsed info from a GitHub URL (repo page or tree URL).
/// </summary>
public record DeployDraft(
    string RepoUrl,
    string Branch = "main",
    string DockerContext = ".",
    string DockerfilePath = "Dockerfile");

/// <summary>
/// Per-user store for the last deploy draft.
/// Key: Sliplane access-token cookie (logged-in) or anonymous browser cookie (pre-login).
/// </summary>
public class DeploymentDraftStore
{
    private static readonly ConcurrentDictionary<string, DeployDraft> _store = new();

    public const string CookieName = "sliplane-deploy-repo-key";

    private readonly IHttpContextAccessor _http;

    public DeploymentDraftStore(IHttpContextAccessor http) => _http = http;

    public DeployDraft? LastDraft => GetDraft();

    /// <summary>
    /// Returns the draft and immediately removes it from the store (one-shot pre-fill).
    /// </summary>
    public DeployDraft? ReadAndClearDraft()
    {
        var key = GetCurrentKey();
        if (key is null) return null;
        _store.TryRemove(key, out var draft);
        return draft;
    }

    public void SaveDraft(DeployDraft draft)
    {
        _store[GetOrCreateKey()] = draft;
    }

    /// <summary>
    /// Parses a GitHub URL into a <see cref="DeployDraft"/>.
    /// Supports:
    ///   https://github.com/{owner}/{repo}/tree/{branch}/{subpath}  → repo + branch + docker context
    ///   https://github.com/{owner}/{repo}                          → repo only
    /// </summary>
    public static DeployDraft ParseGitHubUrl(string input)
    {
        input = input.Trim().TrimEnd('/');

        var treeMatch = Regex.Match(input,
            @"^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/tree/(?<branch>[^/]+)(?:/(?<path>.+))?$");

        if (treeMatch.Success)
        {
            var repoUrl = $"https://github.com/{treeMatch.Groups["owner"].Value}/{treeMatch.Groups["repo"].Value}";
            var branch = treeMatch.Groups["branch"].Value;
            var subPath = treeMatch.Groups["path"].Value.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(subPath))
                return new DeployDraft(repoUrl, branch);

            return new DeployDraft(
                RepoUrl: repoUrl,
                Branch: branch,
                DockerContext: subPath,
                DockerfilePath: $"{subPath}/Dockerfile");
        }

        return new DeployDraft(input);
    }

    private DeployDraft? GetDraft()
    {
        var key = GetCurrentKey();
        return key is not null && _store.TryGetValue(key, out var d) ? d : null;
    }

    private string? GetCurrentKey()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        var token = ctx.Request.Cookies[".ivy.auth.token"];
        if (!string.IsNullOrWhiteSpace(token)) return "token:" + token;

        return ctx.Request.Cookies.TryGetValue(CookieName, out var k) && !string.IsNullOrWhiteSpace(k) ? k : null;
    }

    private string GetOrCreateKey()
    {
        var ctx = _http.HttpContext;

        if (ctx is not null)
        {
            var token = ctx.Request.Cookies[".ivy.auth.token"];
            if (!string.IsNullOrWhiteSpace(token)) return "token:" + token;

            if (ctx.Request.Cookies.TryGetValue(CookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
                return existing;
        }

        var newKey = "anon:" + Guid.NewGuid().ToString("N");
        ctx?.Response.Cookies.Append(CookieName, newKey, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
        });

        return newKey;
    }
}
