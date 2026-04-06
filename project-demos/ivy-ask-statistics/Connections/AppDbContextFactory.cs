using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IvyAskStatistics.Connections;

public sealed class AppDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly IConfiguration _config;
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public AppDbContextFactory(IConfiguration config)
    {
        _config = config;
        EnsureInitialized();
    }

    public AppDbContext CreateDbContext()
    {
        var ctx = new AppDbContext(BuildOptions());
        EnsureInitialized(ctx);
        return ctx;
    }

    private void EnsureInitialized()
    {
        using var ctx = new AppDbContext(BuildOptions());
        EnsureInitialized(ctx);
    }

    private void EnsureInitialized(AppDbContext ctx)
    {
        if (_initialized) return;
        _initLock.Wait();
        try
        {
            if (_initialized) return;
            // EnsureCreated() does nothing when the database already exists (e.g. Supabase).
            // Use explicit CREATE TABLE IF NOT EXISTS instead.
            ctx.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS ivy_ask_questions (
                    "Id"           UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
                    "Widget"       VARCHAR(100) NOT NULL,
                    "Category"     VARCHAR(100) NOT NULL DEFAULT '',
                    "Difficulty"   VARCHAR(10)  NOT NULL,
                    "QuestionText" TEXT         NOT NULL,
                    "Source"       VARCHAR(20)  NOT NULL DEFAULT 'manual',
                    "CreatedAt"    TIMESTAMPTZ  NOT NULL DEFAULT NOW()
                );
                ALTER TABLE ivy_ask_questions ADD COLUMN IF NOT EXISTS "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;

                CREATE TABLE IF NOT EXISTS ivy_ask_test_runs (
                    "Id"             UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
                    "IvyVersion"     VARCHAR(50)  NOT NULL DEFAULT '',
                    "Environment"    VARCHAR(50)  NOT NULL DEFAULT 'production',
                    "TotalQuestions"  INTEGER      NOT NULL DEFAULT 0,
                    "SuccessCount"   INTEGER      NOT NULL DEFAULT 0,
                    "NoAnswerCount"  INTEGER      NOT NULL DEFAULT 0,
                    "ErrorCount"     INTEGER      NOT NULL DEFAULT 0,
                    "StartedAt"      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
                    "CompletedAt"    TIMESTAMPTZ
                );
                CREATE INDEX IF NOT EXISTS ix_test_runs_ivy_version ON ivy_ask_test_runs ("IvyVersion");
                ALTER TABLE ivy_ask_test_runs ADD COLUMN IF NOT EXISTS "DifficultyFilter" VARCHAR(20) NOT NULL DEFAULT 'all';
                ALTER TABLE ivy_ask_test_runs ADD COLUMN IF NOT EXISTS "Concurrency" VARCHAR(10) NOT NULL DEFAULT '';

                CREATE TABLE IF NOT EXISTS ivy_ask_test_results (
                    "Id"             UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
                    "TestRunId"      UUID         NOT NULL REFERENCES ivy_ask_test_runs("Id") ON DELETE CASCADE,
                    "QuestionId"     UUID         NOT NULL REFERENCES ivy_ask_questions("Id") ON DELETE CASCADE,
                    "ResponseText"   TEXT         NOT NULL DEFAULT '',
                    "ResponseTimeMs" INTEGER      NOT NULL DEFAULT 0,
                    "IsSuccess"      BOOLEAN      NOT NULL DEFAULT FALSE,
                    "HttpStatus"     INTEGER      NOT NULL DEFAULT 0,
                    "ErrorMessage"   VARCHAR(500),
                    "CreatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS ix_test_results_run_id      ON ivy_ask_test_results ("TestRunId");
                CREATE INDEX IF NOT EXISTS ix_test_results_question_id ON ivy_ask_test_results ("QuestionId");
                """);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private DbContextOptions<AppDbContext> BuildOptions()
    {
        var cs = _config["DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException(
                "DB_CONNECTION_STRING not set. Run: dotnet user-secrets set \"DB_CONNECTION_STRING\" \"<your connection string>\"");

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;
    }
}
