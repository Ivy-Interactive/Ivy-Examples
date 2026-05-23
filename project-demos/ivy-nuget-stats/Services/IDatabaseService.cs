using IvyInsights.Models;

namespace IvyInsights.Services;

public interface IDatabaseService
{
    Task<List<DailyDownloadStats>> GetDailyDownloadStatsAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<List<GithubStarsStats>> GetGithubStarsStatsAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<List<GithubStargazersDailyStats>> GetGithubStargazersDailyStatsAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<List<GithubStargazer>> GetGithubStargazersAsync(CancellationToken cancellationToken = default);
}

