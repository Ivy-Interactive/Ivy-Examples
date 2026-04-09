namespace IvyBenchmark.Connections;

[Table("benchmark_runs")]
public class BenchmarkRunEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(50)]
    public string IvyVersion { get; set; } = "";

    [MaxLength(50)]
    public string Environment { get; set; } = "local";

    [MaxLength(100)]
    public string? RunnerName { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<BenchmarkResultEntity> Results { get; set; } = [];
}
