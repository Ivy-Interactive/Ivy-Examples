namespace IvyAskStatistics.Models;

public record TestQuestion(string Id, string Widget, string Difficulty, string Question);

public record QuestionRun(
    TestQuestion Question,
    string Status,          // "success" | "no_answer" | "error"
    int ResponseTimeMs,
    int HttpStatus,
    string AnswerText = ""  // raw response body; empty when no_answer or error
);

public record QuestionRow(
    string Id,
    string Widget,
    string Difficulty,
    string Question,
    Icons ResultIcon,
    string Status,
    string Time
);

/// <summary>Latest row in <c>ivy_ask_test_runs</c> (for Run Tests summary UI).</summary>
public sealed record LastSavedRunSummary(
    Guid Id,
    string IvyVersion,
    string Environment,
    string DifficultyFilter,
    string Concurrency,
    int TotalQuestions,
    int SuccessCount,
    int NoAnswerCount,
    int ErrorCount,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<LastSavedRunResultRow> Rows);

public sealed record LastSavedRunResultRow(
    string Widget,
    string Difficulty,
    string QuestionPreview,
    string Outcome,
    int ResponseTimeMs);
