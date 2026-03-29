using System.Diagnostics;

namespace IvyAskStatistics.Services;

public static class IvyAskService
{
    private const string McpBase = "https://mcp.ivy.app";
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Sends a question to the IVY Ask API and returns the result with timing.
    /// GET {baseUrl}/questions?question={encoded}
    ///
    /// Status codes:
    ///   200 + body  → "success"    (answer found)
    ///   404         → "no_answer"  (NO_ANSWER_FOUND)
    ///   other       → "error"
    /// </summary>
    public static async Task<QuestionRun> AskAsync(TestQuestion question, string baseUrl)
    {
        var encoded = Uri.EscapeDataString(question.Question);
        var url = $"{baseUrl}/questions?question={encoded}";

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

            return new QuestionRun(question, status, ms, httpStatus);
        }
        catch
        {
            sw.Stop();
            return new QuestionRun(question, "error", (int)sw.ElapsedMilliseconds, 0);
        }
    }

    /// <summary>
    /// Fetches the full list of Ivy widgets from the docs API.
    /// Returns widgets only (filters out non-widget topics).
    /// </summary>
    public static async Task<List<IvyWidget>> GetWidgetsAsync()
    {
        var yaml = await _http.GetStringAsync($"{McpBase}/docs");
        var widgets = new List<IvyWidget>();

        var lines = yaml.Split('\n');
        for (var i = 0; i < lines.Length - 1; i++)
        {
            var nameLine = lines[i].Trim();
            if (!nameLine.StartsWith("- name: Ivy.Widgets.")) continue;

            var fullName = nameLine["- name: ".Length..];
            var linkLine = lines[i + 1].Trim();
            var link = linkLine.StartsWith("link: ") ? linkLine["link: ".Length..] : "";

            // "Ivy.Widgets.Common.Button" → parts = ["Ivy", "Widgets", "Common", "Button"]
            var parts = fullName.Split('.');
            if (parts.Length < 4) continue;

            var category = parts[2];
            var name = parts[3];

            widgets.Add(new IvyWidget(name, category, link.Trim()));
        }

        return widgets.OrderBy(w => w.Category).ThenBy(w => w.Name).ToList();
    }

    /// <summary>
    /// Fetches the Markdown documentation for a specific widget.
    /// </summary>
    public static async Task<string> GetWidgetDocsAsync(string docLink)
    {
        return await _http.GetStringAsync($"{McpBase}/{docLink}");
    }
}
