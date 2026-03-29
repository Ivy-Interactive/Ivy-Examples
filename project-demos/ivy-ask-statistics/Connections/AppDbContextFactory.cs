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
        var cs = _config["DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException(
                "DB_CONNECTION_STRING not set. Run: dotnet user-secrets set \"DB_CONNECTION_STRING\" \"<your connection string>\"");

        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs)
                .Options);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initLock.Wait();
        try
        {
            if (_initialized) return;
            using var ctx = CreateDbContext();
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
                """);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
