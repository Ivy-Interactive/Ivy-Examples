using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IvyAskStatistics.Connections;

[Table("ivy_ask_test_results")]
public class TestRunResultEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TestRunId { get; set; }
    public TestRunEntity TestRun { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public QuestionEntity Question { get; set; } = null!;

    public string ResponseText { get; set; } = "";
    public int ResponseTimeMs { get; set; }
    public bool IsSuccess { get; set; }
    public int HttpStatus { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
