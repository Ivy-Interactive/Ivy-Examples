namespace IvyAskStatistics.Connections;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<QuestionEntity> Questions { get; set; }
    public DbSet<TestRunEntity> TestRuns { get; set; }
    public DbSet<TestRunResultEntity> TestResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestRunResultEntity>(e =>
        {
            e.HasOne(r => r.TestRun)
                .WithMany(tr => tr.Results)
                .HasForeignKey(r => r.TestRunId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Question)
                .WithMany(q => q.TestResults)
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.TestRunId);
            e.HasIndex(r => r.QuestionId);
        });

        modelBuilder.Entity<TestRunEntity>(e =>
        {
            e.HasIndex(r => r.IvyVersion);
        });

        modelBuilder.Entity<QuestionEntity>(e =>
        {
            e.HasIndex(q => q.IsActive);
        });
    }
}
