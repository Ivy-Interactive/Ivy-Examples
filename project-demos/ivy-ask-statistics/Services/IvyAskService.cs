using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using IvyAskStatistics.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IvyAskStatistics.Services;

public static class IvyAskService
{
    public const string DefaultMcpBaseUrl = "https://mcp.ivy.app";

    /// <summary>
    /// Sent as <c>client=</c> on every MCP HTTP request when no override is provided.
    /// Use a stable slug: lowercase, ASCII, hyphens (e.g. <c>ivy-internal</c>, <c>acme-corp-qa</c>).
    /// </summary>
    public const string DefaultMcpClientId = "ivy-internal";

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };

    /// <summary>Builds <c>GET .../questions?question=...&amp;client=...</c> (client is always included).</summary>
    public static string BuildQuestionsUrl(string baseUrl, string questionOrPrompt, string? client = null)
    {
        var root = baseUrl.TrimEnd('/');
        var q = Uri.EscapeDataString(questionOrPrompt);
        var id = NormalizeMcpClient(client);
        return $"{root}/questions?question={q}&client={Uri.EscapeDataString(id)}";
    }

    /// <summary>Normalizes override or returns <see cref="DefaultMcpClientId"/>.</summary>
    public static string NormalizeMcpClient(string? client)
    {
        var t = (client ?? "").Trim();
        if (t.Length == 0) return DefaultMcpClientId;
        if (t.Length > 120) return t[..120];
        return t;
    }

    /// <summary>
    /// MCP <c>client</c> query value from <c>Mcp:ClientId</c> (user secrets, env, appsettings).
    /// If unset or blank, uses <see cref="DefaultMcpClientId"/>.
    /// </summary>
    public static string ResolveMcpClientId(IConfiguration configuration) =>
        NormalizeMcpClient(configuration["Mcp:ClientId"]);

    /// <summary>Appends <c>client=</c> to any MCP URL (e.g. <c>/docs</c>, widget doc path).</summary>
    public static string WithMcpClientQuery(string absoluteOrRootRelativeUrl, string? client = null)
    {
        var id = NormalizeMcpClient(client);
        var url = absoluteOrRootRelativeUrl.Trim();
        var sep = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{sep}client={Uri.EscapeDataString(id)}";
    }

    /// <summary>
    /// Sends a question to the IVY Ask API and returns the result with timing.
    /// GET {baseUrl}/questions?question={encoded}&amp;client={client}
    ///
    /// Status codes:
    ///   200 + body  → "success"    (answer found)
    ///   404         → "no_answer"  (NO_ANSWER_FOUND)
    ///   other       → "error"
    /// </summary>
    public static async Task<QuestionRun> AskAsync(TestQuestion question, string baseUrl, string? client = null)
    {
        var url = BuildQuestionsUrl(baseUrl, question.Question, client);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.GetAsync(url);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync();
            var ms = (int)sw.ElapsedMilliseconds;
            var httpStatus = (int)response.StatusCode;

            var status = response.StatusCode switch
            {
                HttpStatusCode.OK when !string.IsNullOrWhiteSpace(body) => "success",
                HttpStatusCode.NotFound => "no_answer",
                _ => "error"
            };

            var answerText = status == "success" ? body : "";
            return new QuestionRun(question, status, ms, httpStatus, answerText);
        }
        catch
        {
            sw.Stop();
            return new QuestionRun(question, "error", (int)sw.ElapsedMilliseconds, 0);
        }
    }

    /// <summary>
    /// Calls Ivy Ask with a meta-prompt (e.g. “output JSON array of test questions”).
    /// Same endpoint as normal Ask; the service returns generated text (often JSON).
    /// </summary>
    public static async Task<string?> FetchAnswerBodyAsync(
        string baseUrl, string prompt, CancellationToken ct = default, string? client = null)
    {
        var url = BuildQuestionsUrl(baseUrl, prompt, client);
        using var response = await _http.GetAsync(url, ct);
        if (response.StatusCode != HttpStatusCode.OK) return null;
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Parses Ivy Ask response body into a list of question strings.
    /// Handles raw JSON array, <c>{"questions":[...]}</c>, or fenced markdown code blocks.
    /// </summary>
    public static List<string> ParseQuestionStringsFromBody(string body, int take = 10)
    {
        if (string.IsNullOrWhiteSpace(body)) return [];

        var trimmed = body.Trim();

        // Strip ```json ... ``` if present
        var fence = Regex.Match(trimmed, @"^```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        if (fence.Success)
            trimmed = fence.Groups[1].Value.Trim();

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
                return TakeStrings(root, take);

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var name in new[] { "questions", "items", "data" })
                {
                    if (root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        return TakeStrings(arr, take);
                }
            }
        }
        catch (JsonException)
        {
            // One question per non-empty line
            return trimmed
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.TrimStart('-', ' ', '\t', '"').TrimEnd('"', ','))
                .Where(l => l.Length > 5)
                .Take(take)
                .ToList();
        }

        return [];
    }

    private static List<string> TakeStrings(JsonElement array, int take) =>
        array.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : e.GetRawText())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(take)
            .ToList();

    /// <summary>
    /// Fetches the full list of Ivy widgets from the docs API.
    /// Returns widgets only (filters out non-widget topics).
    /// </summary>
    public static async Task<List<IvyWidget>> GetWidgetsAsync(string? client = null)
    {
        var yaml = await _http.GetStringAsync(WithMcpClientQuery($"{DefaultMcpBaseUrl}/docs", client));
        var widgets = new List<IvyWidget>();

        var lines = yaml.Split('\n');
        for (var i = 0; i < lines.Length - 1; i++)
        {
            var nameLine = lines[i].Trim();
            if (!nameLine.StartsWith("- name: Ivy.Widgets.")) continue;

            var fullName = nameLine["- name: ".Length..];
            var linkLine = lines[i + 1].Trim();
            var link = linkLine.StartsWith("link: ") ? linkLine["link: ".Length..] : "";

            var parts = fullName.Split('.');
            if (parts.Length < 4) continue;

            var category = parts[2];
            var name = parts[3];

            widgets.Add(new IvyWidget(name, category, link.Trim()));
        }

        return widgets.OrderBy(w => w.Category).ThenBy(w => w.Name).ToList();
    }

    /// <summary>
    /// Widgets from <c>/docs</c> merged with distinct widget names stored in the database.
    /// If the docs API fails or returns nothing, rows still appear from DB (fixes empty table when offline).
    /// </summary>
    public static async Task<List<IvyWidget>> GetMergedWidgetCatalogAsync(
        AppDbContextFactory factory,
        CancellationToken ct = default)
    {
        List<IvyWidget> api = [];
        try
        {
            api = await GetWidgetsAsync();
        }
        catch
        {
            // e.g. network blocked in browser / server
        }

        await using var ctx = factory.CreateDbContext();
        var fromDb = await ctx.Questions
            .AsNoTracking()
            .Where(q => q.Widget != "")
            .GroupBy(q => q.Widget)
            .Select(g => new { Widget = g.Key, Category = g.Select(x => x.Category).FirstOrDefault() })
            .ToListAsync(ct);

        var byName = api.ToDictionary(w => w.Name, w => w);
        foreach (var row in fromDb)
        {
            if (!byName.ContainsKey(row.Widget))
                byName[row.Widget] = new IvyWidget(row.Widget, row.Category ?? "", "");
        }

        return byName.Values
            .OrderBy(w => w.Category)
            .ThenBy(w => w.Name)
            .ToList();
    }

    /// <summary>
    /// Fetches the Markdown documentation for a specific widget.
    /// </summary>
    public static async Task<string> GetWidgetDocsAsync(string docLink, string? client = null)
    {
        var path = docLink.TrimStart('/');
        return await _http.GetStringAsync(WithMcpClientQuery($"{DefaultMcpBaseUrl}/{path}", client));
    }
}
