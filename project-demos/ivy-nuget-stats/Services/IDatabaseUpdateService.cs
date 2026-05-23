namespace IvyInsights.Services;

public record DatabaseUpdateResult(
    bool Success,
    string? ErrorMessage,
    int? StargazersNew,
    int? StargazersLeft,
    int? StargazersReactivated);

public interface IDatabaseUpdateService
{
    Task<DatabaseUpdateResult> UpdateStargazersAsync(
        string repoName,
        CancellationToken cancellationToken = default);
}
