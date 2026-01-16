namespace GitHubWrapped.Services;

/// <summary>
/// Configuration for collecting GitHub statistics
/// </summary>
public class GitHubStatsOptions
{
    /// <summary>
    /// Start date for collecting statistics (default: year 2025)
    /// </summary>
    public DateTime StartDate { get; set; } = new(2025, 1, 1);

    /// <summary>
    /// End date for collecting statistics (default: year 2025)
    /// </summary>
    public DateTime EndDate { get; set; } = new(2025, 12, 31, 23, 59, 59);

    /// <summary>
    /// Maximum number of repositories to process
    /// </summary>
    public int MaxRepositories { get; set; } = 200;

    /// <summary>
    /// Maximum number of pages for pagination (for REST API fallback)
    /// </summary>
    public int MaxPages { get; set; } = 10;

    /// <summary>
    /// Number of items per page (GitHub API maximum is 100)
    /// </summary>
    public int PerPage { get; set; } = 100;

    /// <summary>
    /// Maximum number of programming languages to display
    /// </summary>
    public int MaxLanguages { get; set; } = 7;

    /// <summary>
    /// Maximum number of top repositories
    /// </summary>
    public int MaxTopRepos { get; set; } = 5;

    /// <summary>
    /// Include forked repositories when fetching commits
    /// When using GraphQL API, this filters fork contributions
    /// Set to true to get commits from forks as well (default: true)
    /// </summary>
    public bool IncludeForks { get; set; } = true;

    /// <summary>
    /// Create options for a specific year
    /// </summary>
    public static GitHubStatsOptions ForYear(int year)
    {
        return new GitHubStatsOptions
        {
            StartDate = new DateTime(year, 1, 1),
            EndDate = new DateTime(year, 12, 31, 23, 59, 59)
        };
    }
}

