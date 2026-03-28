using System.Diagnostics;
using IvyAskStatistics;

namespace IvyAskStatistics.Services;

public static class IvyAskService
{
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
}
