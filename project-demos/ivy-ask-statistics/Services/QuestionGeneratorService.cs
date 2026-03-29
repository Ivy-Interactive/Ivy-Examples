using IvyAskStatistics.Connections;
using Microsoft.EntityFrameworkCore;

namespace IvyAskStatistics.Services;

/// <summary>
/// Generates test questions via the same Ivy Ask HTTP API as the runner
/// (<c>GET {base}/questions?question=...</c> on <c>mcp.ivy.app</c>).
/// Uses meta-prompts that ask for a JSON array of question strings; Ivy Ask answers from docs + model.
/// Does not embed full widget markdown in the URL (GET length limits).
/// </summary>
public static class QuestionGeneratorService
{
    /// <summary>
    /// Generates 10 questions per difficulty (easy / medium / hard) for a widget
    /// and saves them to the database, replacing any previously MCP-generated ones.
    /// </summary>
    public static async Task GenerateAndSaveAsync(
        IvyWidget widget,
        AppDbContextFactory factory,
        string askBaseUrl,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        foreach (var difficulty in new[] { "easy", "medium", "hard" })
        {
            progress?.Report($"Ivy Ask: {difficulty} questions for {widget.Name}…");

            var prompt = BuildMetaPrompt(widget.Name, widget.Category, difficulty);
            var body = await IvyAskService.FetchAnswerBodyAsync(askBaseUrl, prompt, ct)
                ?? throw new InvalidOperationException("Ivy Ask returned no body (check network and base URL).");

            var questions = IvyAskService.ParseQuestionStringsFromBody(body, 10);
            if (questions.Count == 0)
                throw new InvalidOperationException(
                    "Could not parse questions from Ivy Ask response. Try again or inspect logs.");

            await using var ctx = factory.CreateDbContext();

            var existing = await ctx.Questions
                .Where(q => q.Widget == widget.Name && q.Difficulty == difficulty && q.Source == "ivy_ask_meta")
                .ToListAsync(ct);
            ctx.Questions.RemoveRange(existing);

            ctx.Questions.AddRange(questions.Select(q => new QuestionEntity
            {
                Widget = widget.Name,
                Category = widget.Category,
                Difficulty = difficulty,
                QuestionText = q,
                Source = "ivy_ask_meta",
                CreatedAt = DateTime.UtcNow
            }));

            await ctx.SaveChangesAsync(ct);
        }
    }

    private static string BuildMetaPrompt(string widgetName, string category, string difficulty)
    {
        var difficultyHint = difficulty switch
        {
            "easy" => "basic usage, creating the widget, simple properties",
            "medium" => "events, styling, configuration, common patterns",
            _ => "advanced composition, edge cases, integration with other Ivy widgets"
        };

        return $"""
            You are generating automated test questions for the Ivy UI framework documentation system.

            Widget name: {widgetName}
            Widget category (folder): {category}
            Difficulty: {difficulty} — {difficultyHint}

            Generate exactly 10 distinct questions that a C# developer might ask about this Ivy widget.
            Each question must be a single sentence, under 25 words, about the "{widgetName}" widget only.
            Do not combine multiple unrelated topics in one question.

            Respond with ONLY a JSON array of 10 strings (no markdown fences, no keys, no commentary).
            Example shape: ["question1?", "question2?", ...]
            """;
    }
}
