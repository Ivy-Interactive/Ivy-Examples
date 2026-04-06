using IvyAskStatistics.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace IvyAskStatistics.Services;

/// <summary>
/// Generates test questions by fetching the full widget Markdown documentation
/// from mcp.ivy.app and passing it to OpenAI as context.
/// Uses <c>OpenAI:ApiKey</c>, <c>OpenAI:BaseUrl</c>, and <c>OpenAI:ChatModel</c> from configuration / user secrets.
/// </summary>
public static class QuestionGeneratorService
{
    public const string ApiKeyConfigKey = "OpenAI:ApiKey";
    public const string BaseUrlConfigKey = "OpenAI:BaseUrl";
    public const string ChatModelConfigKey = "OpenAI:ChatModel";

    /// <summary>Returns <c>null</c> if OpenAI settings are present; otherwise a user-facing message.</summary>
    public static string? GetOpenAiConfigurationError(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration[ApiKeyConfigKey]))
            return $"{ApiKeyConfigKey} is not set. Run: dotnet user-secrets set \"{ApiKeyConfigKey}\" \"<key>\"";
        if (string.IsNullOrWhiteSpace(configuration[BaseUrlConfigKey]))
            return $"{BaseUrlConfigKey} is not set. Run: dotnet user-secrets set \"{BaseUrlConfigKey}\" \"<url>\"";
        if (string.IsNullOrWhiteSpace(configuration[ChatModelConfigKey]?.Trim()))
            return $"{ChatModelConfigKey} is not set. Run: dotnet user-secrets set \"{ChatModelConfigKey}\" \"<model id>\"";
        return null;
    }

    /// <summary>
    /// Generates 10 questions per difficulty (easy / medium / hard) for a widget
    /// and saves them to the database, replacing any previously generated ones.
    /// </summary>
    public static async Task GenerateAndSaveAsync(
        IvyWidget widget,
        AppDbContextFactory factory,
        string openAiApiKey,
        string openAiBaseUrl,
        IConfiguration configuration,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var cfgErr = GetOpenAiConfigurationError(configuration);
        if (cfgErr != null)
            throw new InvalidOperationException(cfgErr);

        progress?.Report($"Fetching docs for {widget.Name}…");

        var markdown = await FetchDocsMarkdownAsync(widget, ct);

        var model = configuration[ChatModelConfigKey]!.Trim();
        var chatClient = BuildChatClient(openAiApiKey, openAiBaseUrl, model);

        foreach (var difficulty in new[] { "easy", "medium", "hard" })
        {
            progress?.Report($"OpenAI: generating {difficulty} questions for {widget.Name}…");

            var messages = BuildMessages(widget, difficulty, markdown);
            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);
            var body = response.Value.Content[0].Text ?? "";

            var questions = IvyAskService.ParseQuestionStringsFromBody(body, 10);
            if (questions.Count == 0)
                throw new InvalidOperationException(
                    $"Could not parse {difficulty} questions from OpenAI response. Body: {body[..Math.Min(200, body.Length)]}");

            await using var ctx = factory.CreateDbContext();

            var existing = await ctx.Questions
                .Where(q => q.Widget == widget.Name && q.Difficulty == difficulty && q.Source == "openai_docs")
                .ToListAsync(ct);
            ctx.Questions.RemoveRange(existing);

            ctx.Questions.AddRange(questions.Select(q => new QuestionEntity
            {
                Widget     = widget.Name,
                Category   = widget.Category,
                Difficulty = difficulty,
                QuestionText = q,
                Source     = "openai_docs",
                CreatedAt  = DateTime.UtcNow
            }));

            await ctx.SaveChangesAsync(ct);
        }
    }

    private static async Task<string> FetchDocsMarkdownAsync(IvyWidget widget, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(widget.DocLink))
            return $"No documentation link available for widget \"{widget.Name}\".";

        try
        {
            return await IvyAskService.GetWidgetDocsAsync(widget.DocLink);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to fetch docs for \"{widget.Name}\" (link: {widget.DocLink}): {ex.Message}", ex);
        }
    }

    private static ChatClient BuildChatClient(string apiKey, string baseUrl, string model)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl.TrimEnd('/'))
        };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model);
    }

    private static List<OpenAI.Chat.ChatMessage> BuildMessages(IvyWidget widget, string difficulty, string markdown)
    {
        var difficultyHint = difficulty switch
        {
            "easy"   => "basic usage, creating the widget, simple properties",
            "medium" => "events, styling, configuration, common patterns",
            _        => "advanced composition, edge cases, integration with other Ivy widgets"
        };

        var system = new SystemChatMessage("""
            You are generating automated test questions for the Ivy UI framework documentation QA system.
            You will receive the full Markdown documentation for a specific Ivy widget.
            Base your questions ONLY on what is documented in the provided content.
            Do not invent APIs, properties, or behaviors not mentioned in the documentation.
            """);

        var user = new UserChatMessage($"""
            Widget name: {widget.Name}
            Widget category: {widget.Category}
            Difficulty: {difficulty} — {difficultyHint}

            --- DOCUMENTATION ---
            {markdown}
            --- END DOCUMENTATION ---

            Generate exactly 10 distinct {difficulty}-level questions that a C# developer might ask about the "{widget.Name}" widget.
            Each question must be a single sentence, under 25 words, and grounded in the documentation above.
            Do not combine multiple unrelated topics in one question.

            Respond with ONLY a JSON array of 10 strings (no markdown fences, no keys, no commentary).
            Example shape: ["question1?", "question2?", ...]
            """);

        return [system, user];
    }
}
