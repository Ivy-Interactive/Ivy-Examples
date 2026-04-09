namespace IvyBenchmark.Connections;

[Table("benchmark_results")]
public class BenchmarkResultEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RunId { get; set; }
    public BenchmarkRunEntity Run { get; set; } = null!;

    [MaxLength(100)]
    public string ScenarioKey { get; set; } = "";

    [MaxLength(200)]
    public string ScenarioLabel { get; set; } = "";

    [MaxLength(20)]
    public string MetricKind { get; set; } = "";

    public double ValueMs { get; set; }

    public double? P95Ms { get; set; }

    public double? MinMs { get; set; }

    public double? MaxMs { get; set; }

    public int Iterations { get; set; } = 1;

    public bool Success { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
}
