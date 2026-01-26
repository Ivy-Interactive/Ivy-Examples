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
}

