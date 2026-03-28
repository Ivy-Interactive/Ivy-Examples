namespace IvyAskStatistics;

public record TestQuestion(
    string Id,
    string Widget,
    string Difficulty,
    string Question
);

public record QuestionRun(
    TestQuestion Question,
    string Status,      // "success" | "no_answer" | "error"
    int ResponseTimeMs,
    int HttpStatus
);

// Flat row used for the TableBuilder display
public record QuestionRow(
    string Id,
    string Widget,
    string Difficulty,
    string Question,
    string Status,
    int? ResponseTimeMs
);
