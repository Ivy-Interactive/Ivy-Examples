namespace IvyAskStatistics.Models;

public record TestQuestion(string Id, string Widget, string Difficulty, string Question);

public record QuestionRun(
    TestQuestion Question,
    string Status,      // "success" | "no_answer" | "error"
    int ResponseTimeMs,
    int HttpStatus
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
