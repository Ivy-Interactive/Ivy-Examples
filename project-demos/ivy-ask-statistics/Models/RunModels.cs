namespace IvyAskStatistics.Models;

public record TestQuestion(string Id, string Widget, string Difficulty, string Question);

public record QuestionRun(
    TestQuestion Question,
    string Status,      // "success" | "no_answer" | "error"
    int ResponseTimeMs,
    int HttpStatus
);

// Flat row for the TableBuilder in RunApp
public record QuestionRow(
    string Id,
    string Widget,
    string Difficulty,
    string Question,
    string Status,
    int? ResponseTimeMs
);
