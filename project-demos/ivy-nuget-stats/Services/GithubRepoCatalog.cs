namespace IvyInsights.Services;

/// <summary>Maps NuGet package tabs to GitHub repositories tracked in Postgres.</summary>
public static class GithubRepoCatalog
{
    public const string IvyFramework = "Ivy-Interactive/Ivy-Framework";
    public const string IvyTendril = "Ivy-Interactive/Ivy-Tendril";

    public static readonly IReadOnlyList<string> AllRepos = [IvyFramework, IvyTendril];

    public static string GetRepoForPackage(string packageId) => packageId switch
    {
        "Ivy" => IvyFramework,
        "Ivy.Tendril" => IvyTendril,
        _ => IvyFramework,
    };

    public static string GetDisplayName(string repoName) => repoName switch
    {
        IvyFramework => "Ivy Framework",
        IvyTendril => "Ivy Tendril",
        _ => repoName,
    };
}
