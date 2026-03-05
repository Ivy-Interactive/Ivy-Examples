namespace SliplaneManage.Services;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Per-user store for the deployment repo URL.
/// Key: access token (logged-in) or anonymous browser cookie (pre-login).
/// </summary>
public class DeploymentDraftStore
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _store = new();

    public const string CookieName = "sliplane-deploy-repo-key";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeploymentDraftStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? LastRepoUrl => GetRepoUrl();

    public void SaveRepoUrl(string? repoUrl)
    {
        if (string.IsNullOrWhiteSpace(repoUrl)) return;
        _store[GetOrCreateKey()] = repoUrl;
    }

    private string? GetRepoUrl()
    {
        var key = GetCurrentKey();
        return key is not null && _store.TryGetValue(key, out var url) ? url : null;
    }

    private string? GetCurrentKey()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        // Prefer access token (per authenticated user)
        var token = ctx.Request.Cookies[".ivy.auth.token"];
        if (!string.IsNullOrWhiteSpace(token)) return "token:" + token;

        // Fall back to anonymous browser cookie
        return ctx.Request.Cookies.TryGetValue(CookieName, out var k) && !string.IsNullOrWhiteSpace(k) ? k : null;
    }

    private string GetOrCreateKey()
    {
        var ctx = _httpContextAccessor.HttpContext;

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
