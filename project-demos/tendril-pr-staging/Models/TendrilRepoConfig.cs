namespace TendrilPrStaging.Models;

/// <summary>GitHub repository configured for Tendril PR staging deploys.</summary>
public sealed class TendrilRepoConfig
{
    /// <summary>Stable slug used in Sliplane service names. Lowercase, hyphenated.</summary>
    public string Key { get; set; } = "";

    /// <summary>GitHub owner (org/user) where PRs live.</summary>
    public string Owner { get; set; } = "";

    /// <summary>GitHub repo name where PRs live.</summary>
    public string Repo { get; set; } = "";

    /// <summary>Git clone URL for the repo. Defaults to https://github.com/{Owner}/{Repo}.</summary>
    public string GitUrl => $"https://github.com/{Owner}/{Repo}";

    /// <summary>Path to the Tendril Dockerfile within the repo.</summary>
    public string DockerfilePath { get; set; } = ".github/docker/Dockerfile.tendril-source";

    /// <summary>Docker build context within the repo.</summary>
    public string DockerContext { get; set; } = ".";
}
