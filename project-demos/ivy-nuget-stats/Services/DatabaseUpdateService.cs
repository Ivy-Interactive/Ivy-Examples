using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace IvyInsights.Services;

public class DatabaseUpdateService : IDatabaseUpdateService
{
    private const string Repo = "Ivy-Interactive/Ivy-Framework";
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;

    public DatabaseUpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ivy-stars-tracker");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.star+json");

        var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? new ConfigurationBuilder().AddUserSecrets(typeof(DatabaseService).Assembly).Build()["DB_CONNECTION_STRING"];
        
        _connectionString = new NpgsqlConnectionStringBuilder(connStr 
            ?? throw new InvalidOperationException("DB_CONNECTION_STRING not set")).ConnectionString;
    }

    public async Task<DatabaseUpdateResult> UpdateStargazersAsync(CancellationToken ct = default)
    {
        try
        {
            var current = await FetchStargazersAsync(ct);
            if (current.Count == 0) return new DatabaseUpdateResult(false, "No stargazers found", 0, 0, 0);

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var previous = await GetActiveUsersAsync(conn, ct);
            var newUsers = current.Keys.Except(previous).ToList();
            var leftUsers = previous.Except(current.Keys).ToList();

            if (newUsers.Count > 0)
                await InsertNewUsersAsync(conn, newUsers, current, ct);

            var reactivated = current.Keys.Intersect(previous).Count() > 0 
                ? await ReactivateUsersAsync(conn, current.Keys, ct) : 0;

            if (leftUsers.Count > 0)
                await MarkLeftUsersAsync(conn, leftUsers, ct);

            await UpsertDailyStatsAsync(conn, newUsers.Count, leftUsers.Count, reactivated, ct);

            return new DatabaseUpdateResult(true, null, newUsers.Count, leftUsers.Count, reactivated);
        }
        catch (Exception ex)
        {
            return new DatabaseUpdateResult(false, ex.Message, null, null, null);
        }
    }

    private async Task<Dictionary<string, DateTime?>> FetchStargazersAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, DateTime?>();
        for (var page = 1; ; page++)
        {
            var data = await _httpClient.GetFromJsonAsync<StargazerApiItem[]>(
                $"https://api.github.com/repos/{Repo}/stargazers?per_page=100&page={page}", ct);
            
            if (data == null || data.Length == 0) break;
            
            foreach (var item in data.Where(x => !string.IsNullOrEmpty(x.User?.Login)))
                result[item.User!.Login!] = item.StarredAt;
        }
        return result;
    }

    private static async Task<HashSet<string>> GetActiveUsersAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT user_login FROM github_stargazers WHERE repo_name = @repo AND unstarred_at IS NULL", conn);
        cmd.Parameters.AddWithValue("repo", Repo);
        
        var result = new HashSet<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(reader.GetString(0));
        return result;
    }

    private static async Task InsertNewUsersAsync(NpgsqlConnection conn, List<string> users, 
        Dictionary<string, DateTime?> starredMap, CancellationToken ct)
    {
        var logins = users.ToArray();
        var dates = users.Select(u => starredMap.TryGetValue(u, out var d) && d.HasValue ? d : null).ToArray();

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO github_stargazers (repo_name, user_login, starred_at, unstarred_at)
            SELECT @repo, unnest(@logins), unnest(@dates), NULL
            ON CONFLICT (repo_name, user_login) DO NOTHING", conn);

        cmd.Parameters.AddWithValue("repo", Repo);
        cmd.Parameters.Add("logins", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = logins;
        cmd.Parameters.Add("dates", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz).Value = dates;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> ReactivateUsersAsync(NpgsqlConnection conn, IEnumerable<string> users, CancellationToken ct)
    {
        var logins = users.ToArray();
        await using var cmd = new NpgsqlCommand(@"
            UPDATE github_stargazers SET unstarred_at = NULL
            WHERE repo_name = @repo AND user_login = ANY(@logins) AND unstarred_at IS NOT NULL", conn);
        cmd.Parameters.AddWithValue("repo", Repo);
        cmd.Parameters.Add("logins", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = logins;
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task MarkLeftUsersAsync(NpgsqlConnection conn, List<string> users, CancellationToken ct)
    {
        var logins = users.ToArray();
        await using var cmd = new NpgsqlCommand(@"
            UPDATE github_stargazers SET unstarred_at = @now
            WHERE repo_name = @repo AND user_login = ANY(@logins) AND unstarred_at IS NULL", conn);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("repo", Repo);
        cmd.Parameters.Add("logins", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = logins;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertDailyStatsAsync(NpgsqlConnection conn, int newCount, int leftCount, int reactivated, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO github_stargazers_daily (repo_name, date, new_count, unstar_count, reactivated_count)
            VALUES (@repo, @date, @new, @left, @react)
            ON CONFLICT (repo_name, date) DO UPDATE SET
                new_count = EXCLUDED.new_count, unstar_count = EXCLUDED.unstar_count, reactivated_count = EXCLUDED.reactivated_count", conn);
        cmd.Parameters.AddWithValue("repo", Repo);
        cmd.Parameters.AddWithValue("date", DateOnly.FromDateTime(DateTime.UtcNow));
        cmd.Parameters.AddWithValue("new", newCount);
        cmd.Parameters.AddWithValue("left", leftCount);
        cmd.Parameters.AddWithValue("react", reactivated);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private sealed class StargazerApiItem
    {
        [JsonPropertyName("user")] public GitHubUser? User { get; set; }
        [JsonPropertyName("starred_at")] public DateTime? StarredAt { get; set; }
    }

    private sealed class GitHubUser
    {
        [JsonPropertyName("login")] public string? Login { get; set; }
    }
}
