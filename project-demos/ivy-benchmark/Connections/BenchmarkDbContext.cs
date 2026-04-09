namespace IvyBenchmark.Connections;

public class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : DbContext(options)
{
    public DbSet<BenchmarkRunEntity> Runs { get; set; }
    public DbSet<BenchmarkResultEntity> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenchmarkResultEntity>(e =>
        {
            e.HasOne(r => r.Run)
                .WithMany(run => run.Results)
                .HasForeignKey(r => r.RunId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.RunId);
            e.HasIndex(r => r.ScenarioKey);
            e.HasIndex(r => new { r.ScenarioKey, r.RunId });
        });

        modelBuilder.Entity<BenchmarkRunEntity>(e =>
        {
            e.HasIndex(r => r.IvyVersion);
        });
    }
}
