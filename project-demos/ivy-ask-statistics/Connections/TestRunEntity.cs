using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IvyAskStatistics.Connections;

[Table("ivy_ask_test_runs")]
public class TestRunEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(50)]
    public string IvyVersion { get; set; } = "";

    [MaxLength(50)]
    public string Environment { get; set; } = "production";

    public int TotalQuestions { get; set; }
    public int SuccessCount { get; set; }
    public int NoAnswerCount { get; set; }
    public int ErrorCount { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public List<TestResultEntity> Results { get; set; } = [];
}
