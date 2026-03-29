using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IvyAskStatistics.Connections;
using Microsoft.EntityFrameworkCore;

namespace IvyAskStatistics.Services;

public static class QuestionGeneratorService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// Generates 10 questions per difficulty (easy / medium / hard) for a widget
    /// and saves them to the database, replacing any previously generated ones.
    /// </summary>
    public static async Task GenerateAndSaveAsync(
        IvyWidget widget,
        string openAiKey,
        AppDbContextFactory factory,
        IProgress<string>? progress = null)
    {
        var docs = await IvyAskService.GetWidgetDocsAsync(widget.DocLink);

        foreach (var difficulty in new[] { "easy", "medium", "hard" })
        {
            progress?.Report($"Generating {difficulty} questions for {widget.Name}…");

            var questions = await GenerateQuestionsAsync(widget.Name, docs, difficulty, openAiKey);

            await using var ctx = factory.CreateDbContext();

            var existing = await ctx.Questions
                .Where(q => q.Widget == widget.Name && q.Difficulty == difficulty && q.Source == "generated")
                .ToListAsync();
            ctx.Questions.RemoveRange(existing);

            ctx.Questions.AddRange(questions.Select(q => new QuestionEntity
            {
                Widget = widget.Name,
                Category = widget.Category,
                Difficulty = difficulty,
                QuestionText = q,
                Source = "generated",
                CreatedAt = DateTime.UtcNow
            }));

            await ctx.SaveChangesAsync();
        }
    }

    private static async Task<List<string>> GenerateQuestionsAsync(
        string widgetName,
        string docs,
        string difficulty,
        string openAiKey)
    {
        var prompt = $$"""
            You are an expert in the Ivy Framework for C#.

            Here is the documentation for the "{{widgetName}}" widget:

            {{docs}}

            Generate exactly 10 {{difficulty}} questions that a developer might ask about this widget.

            DIFFICULTY GUIDELINES:
            - easy:   Basic usage ("how to create", "how to show", simple properties)
            - medium: Specific features, configuration, event handlers, styling
            - hard:   Advanced patterns, combining with other widgets, dynamic data, edge cases

            Rules:
            1. Each question must be specific and about the {{widgetName}} widget in Ivy
            2. No compound questions - one concept per question
            3. Keep questions concise (under 20 words)
            4. Return ONLY valid JSON in this exact format: {"questions": ["q1", "q2", "q3"]}
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, OpenAiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);
        request.Content = JsonContent.Create(new
        {
            model = "gpt-4o-mini",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7,
            response_format = new { type = "json_object" }
        });

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<JsonElement>();
        var content = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        var parsed = JsonSerializer.Deserialize<JsonElement>(content);
        return parsed
            .GetProperty("questions")
            .EnumerateArray()
            .Select(q => q.GetString() ?? "")
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Take(10)
            .ToList();
    }
}
