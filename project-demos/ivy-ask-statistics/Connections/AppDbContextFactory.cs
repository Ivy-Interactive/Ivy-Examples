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
                ALTER TABLE ivy_ask_questions ADD COLUMN IF NOT EXISTS "LastRunStatus"        VARCHAR(20);
                ALTER TABLE ivy_ask_questions ADD COLUMN IF NOT EXISTS "LastRunResponseTimeMs" INTEGER;
                ALTER TABLE ivy_ask_questions ADD COLUMN IF NOT EXISTS "LastRunHttpStatus"     INTEGER;
                ALTER TABLE ivy_ask_questions ADD COLUMN IF NOT EXISTS "LastRunAnswerText"     TEXT;
                ALTER TABLE ivy_ask_questions ADD COLUMN IF NOT EXISTS "LastRunAt"             TIMESTAMPTZ;
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
