namespace IvyAskStatistics.Models;

public record QuestionDetailRow(
    Guid Id,
    string Difficulty,
    string Category,
    string QuestionText,
    string Source,
    string CreatedAt);
