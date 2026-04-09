namespace IvyBenchmark.Connections;

public sealed class BenchmarkDbContextFactory : IDbContextFactory<BenchmarkDbContext>
{
    private readonly IConfiguration _config;
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public BenchmarkDbContextFactory(IConfiguration config)
    {
        _config = config;
        EnsureInitialized();
    }

    public BenchmarkDbContext CreateDbContext()
    {
        var ctx = new BenchmarkDbContext(BuildOptions());
        EnsureInitialized(ctx);
        return ctx;
    }

    private void EnsureInitialized()
    {
        using var ctx = new BenchmarkDbContext(BuildOptions());
        EnsureInitialized(ctx);
    }

    private void EnsureInitialized(BenchmarkDbContext ctx)
    {
        if (_initialized) return;
        _initLock.Wait();
        try
        {
            if (_initialized) return;
            ctx.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS benchmark_runs (
                    "Id"          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
                    "IvyVersion"  VARCHAR(50)  NOT NULL DEFAULT '',
                    "Environment" VARCHAR(50)  NOT NULL DEFAULT 'local',
                    "RunnerName"  VARCHAR(100),
                    "StartedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
                    "CompletedAt" TIMESTAMPTZ,
                    "Notes"       VARCHAR(500)
                );
                CREATE INDEX IF NOT EXISTS ix_benchmark_runs_ivy_version
                    ON benchmark_runs ("IvyVersion");

                CREATE TABLE IF NOT EXISTS benchmark_results (
                    "Id"            UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
                    "RunId"         UUID         NOT NULL REFERENCES benchmark_runs("Id") ON DELETE CASCADE,
                    "ScenarioKey"   VARCHAR(100) NOT NULL DEFAULT '',
                    "ScenarioLabel" VARCHAR(200) NOT NULL DEFAULT '',
                    "MetricKind"    VARCHAR(20)  NOT NULL DEFAULT '',
                    "ValueMs"       DOUBLE PRECISION NOT NULL DEFAULT 0,
                    "P95Ms"         DOUBLE PRECISION,
                    "MinMs"         DOUBLE PRECISION,
                    "MaxMs"         DOUBLE PRECISION,
                    "Iterations"    INTEGER      NOT NULL DEFAULT 1,
                    "Success"       BOOLEAN      NOT NULL DEFAULT FALSE,
                    "ErrorMessage"  VARCHAR(500)
                );
                CREATE INDEX IF NOT EXISTS ix_benchmark_results_run_id
                    ON benchmark_results ("RunId");
                CREATE INDEX IF NOT EXISTS ix_benchmark_results_scenario_key
                    ON benchmark_results ("ScenarioKey");
                CREATE INDEX IF NOT EXISTS ix_benchmark_results_scenario_run
                    ON benchmark_results ("ScenarioKey", "RunId");
                """);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private DbContextOptions<BenchmarkDbContext> BuildOptions()
    {
        var cs = _config["DB_CONNECTION_STRING"]
                 ?? throw new InvalidOperationException(
                     "DB_CONNECTION_STRING not set. Run: dotnet user-secrets set \"DB_CONNECTION_STRING\" \"<your connection string>\"");

        return new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(cs)
            .Options;
    }
}
