namespace PrStagingDeploy.Models;

/// <summary>
/// Single repository configured for PR staging deploys.
/// A repo may have <see cref="Docs"/>, <see cref="Samples"/>, or both. Whichever section is present
/// is the one this repo deploys for each PR.
/// </summary>
public sealed class StagingRepoConfig
{
    /// <summary>Stable slug used in Sliplane service names. Lowercase, hyphenated.</summary>
    public string Key { get; set; } = "";

    /// <summary>GitHub owner (org/user) where PRs live.</summary>
    public string Owner { get; set; } = "";

    /// <summary>GitHub repo name where PRs live.</summary>
    public string Repo { get; set; } = "";

    /// <summary>Docs component (omit to skip docs deploys for this repo).</summary>
    public StagingComponentConfig? Docs { get; set; }

    /// <summary>Samples component (omit to skip samples deploys for this repo).</summary>
    public StagingComponentConfig? Samples { get; set; }

    public bool HasDocs => Docs != null && !string.IsNullOrWhiteSpace(Docs.Repo);
    public bool HasSamples => Samples != null && !string.IsNullOrWhiteSpace(Samples.Repo);
}

/// <summary>
/// One deployable component (docs OR samples) of a <see cref="StagingRepoConfig"/>.
/// </summary>
public sealed class StagingComponentConfig
{
    /// <summary>Git URL Sliplane will clone (typically the same as the PR repo, but can differ for monorepos).</summary>
    public string Repo { get; set; } = "";

    /// <summary>Path to the Dockerfile within the cloned repo.</summary>
    public string Dockerfile { get; set; } = "Dockerfile";

    /// <summary>Docker build context within the cloned repo.</summary>
    public string Context { get; set; } = ".";
}
