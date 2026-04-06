using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IvyAskStatistics.Connections;

[Table("ivy_ask_questions")]
public class QuestionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string Widget { get; set; } = "";

    [MaxLength(100)]
    public string Category { get; set; } = "";

    [MaxLength(10)]
    public string Difficulty { get; set; } = "";

    public string QuestionText { get; set; } = "";

    [MaxLength(20)]
    public string Source { get; set; } = "manual";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TestRunResultEntity> TestResults { get; set; } = [];
}
