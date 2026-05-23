using IvyInsights.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace IvyInsights.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // Try environment variable first, then user secrets
        var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        
        if (string.IsNullOrEmpty(dbConnectionString))
        {
            // Read from user secrets
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(DatabaseService).Assembly)
                .Build();
            
            dbConnectionString = configuration["DB_CONNECTION_STRING"];
        }

        if (string.IsNullOrEmpty(dbConnectionString))
        {
            throw new InvalidOperationException("DB_CONNECTION_STRING is not set. Please set it using: dotnet user-secrets set \"DB_CONNECTION_STRING\" \"your-connection-string\"");
        }

        // NpgsqlConnectionStringBuilder automatically handles both URI and key-value formats
        var builder = new NpgsqlConnectionStringBuilder(dbConnectionString);
        _connectionString = builder.ConnectionString;
    }

    public async Task<List<DailyDownloadStats>> GetDailyDownloadStatsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var stats = new List<DailyDownloadStats>();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // Get data for the last N days, ordered by date
            var query = @"
                SELECT date, downloads
                FROM nuget_history
                ORDER BY date DESC
                LIMIT @days;
            ";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("days", days);
            
            var records = new List<(DateOnly Date, long Downloads)>();
            
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var date = reader.GetDateTime(0).Date;
                var downloads = reader.GetInt64(1);
                records.Add((DateOnly.FromDateTime(date), downloads));
            }

            // Sort by date (oldest to newest) for correct growth calculation
            records = records.OrderBy(r => r.Date).ToList();

            // Calculate daily growth for each day (skip first day as it has no previous day)
            for (int i = 1; i < records.Count; i++)
            {
                var current = records[i];
                var previous = records[i - 1];
                var dailyGrowth = current.Downloads - previous.Downloads;

                stats.Add(new DailyDownloadStats
                {
                    Date = current.Date,
                    TotalDownloads = current.Downloads,
                    DailyGrowth = dailyGrowth
                });
            }

            // Return in reverse order (newest to oldest)
            return stats.OrderByDescending(s => s.Date).ToList();
        }
        catch (Exception ex)
        {
            // Log error and return empty list
            Console.WriteLine($"Error fetching daily download stats: {ex.Message}");
            if (_connectionString != null)
            {
                var preview = _connectionString.Length > 50 
                    ? _connectionString.Substring(0, 50) + "..." 
                    : _connectionString;
                Console.WriteLine($"Connection string: {preview}");
            }
            return new List<DailyDownloadStats>();
        }
    }

    public async Task<List<GithubStarsStats>> GetGithubStarsStatsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var stats = new List<GithubStarsStats>();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // Compute stars per day from github_stargazers: for each date, count users who had starred on or before that date and had not unstarred by end of that date
            var query = @"
                SELECT d::date AS date,
                       (SELECT COUNT(*)::bigint
                        FROM github_stargazers g
                        WHERE g.repo_name = 'Ivy-Interactive/Ivy-Framework'
                          AND (g.starred_at AT TIME ZONE 'UTC')::date <= d::date
                          AND (g.unstarred_at IS NULL OR (g.unstarred_at AT TIME ZONE 'UTC')::date > d::date)) AS stars
                FROM generate_series(
                    CURRENT_DATE - @days,
                    CURRENT_DATE,
                    '1 day'::interval
                ) AS d
                ORDER BY d::date ASC;
            ";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("days", days);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var date = reader.GetDateTime(0).Date;
                var stars = reader.GetInt64(1);
                stats.Add(new GithubStarsStats
                {
                    Date = DateOnly.FromDateTime(date),
                    Stars = stars
                });
            }

            return stats;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching github stars stats: {ex.Message}");
            return new List<GithubStarsStats>();
        }
    }

    public async Task<List<GithubStargazersDailyStats>> GetGithubStargazersDailyStatsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var stats = new List<GithubStargazersDailyStats>();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var query = @"
                SELECT date, new_count, unstar_count, reactivated_count
                FROM github_stargazers_daily
                WHERE repo_name = 'Ivy-Interactive/Ivy-Framework'
                ORDER BY date DESC
                LIMIT @days;
            ";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("days", days);
            
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var date = reader.GetDateTime(0).Date;
                var newCount = reader.GetInt32(1);
                var unstarCount = reader.GetInt32(2);
                var reactivatedCount = reader.GetInt32(3);
                
                stats.Add(new GithubStargazersDailyStats
                {
                    Date = DateOnly.FromDateTime(date),
                    NewCount = newCount,
                    UnstarCount = unstarCount,
                    ReactivatedCount = reactivatedCount
                });
            }

            return stats.OrderByDescending(s => s.Date).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching github stargazers daily stats: {ex.Message}");
            return new List<GithubStargazersDailyStats>();
        }
    }

    public async Task<List<GithubStargazer>> GetGithubStargazersAsync(CancellationToken cancellationToken = default)
    {
        var stargazers = new List<GithubStargazer>();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var query = @"
                SELECT user_login, starred_at, unstarred_at
                FROM github_stargazers
                WHERE repo_name = 'Ivy-Interactive/Ivy-Framework'
                ORDER BY starred_at DESC;
            ";

            await using var cmd = new NpgsqlCommand(query, conn);
            
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var userLogin = reader.GetString(0);
                var starredAt = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                var unstarredAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                
                stargazers.Add(new GithubStargazer
                {
                    Username = userLogin,
                    StarredAt = starredAt,
                    AvatarUrl = null,
                    UnstarredAt = unstarredAt,
                    IsActive = unstarredAt == null
                });
            }

            return stargazers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching github stargazers: {ex.Message}");
            return new List<GithubStargazer>();
        }
    }
}

