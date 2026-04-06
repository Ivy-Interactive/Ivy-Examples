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

    /// <summary>Difficulty scope for the run: <c>all</c>, <c>easy</c>, <c>medium</c>, <c>hard</c>.</summary>
    [MaxLength(20)]
    public string DifficultyFilter { get; set; } = "all";

    [MaxLength(10)]
    public string Concurrency { get; set; } = "";

    public int TotalQuestions { get; set; }
    public int SuccessCount { get; set; }
    public int NoAnswerCount { get; set; }
    public int ErrorCount { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public List<TestRunResultEntity> Results { get; set; } = [];
}
