namespace TendrilUbuntuDeploy.Apps;

using Microsoft.Extensions.Configuration;
using TendrilUbuntuDeploy.Apps.Views;

/// <summary>
/// Git / Docker build source for Sliplane (repo, branch, Dockerfile path, context).
/// Configure with <c>dotnet user-secrets</c> (see README) or environment variables under <see cref="SectionKey"/>.
/// </summary>
public static class UbuntuDeployGitConfiguration
{
    public const string SectionKey = "UbuntuDeploy";

    /// <summary>
    /// Overwrites <paramref name="model"/> git fields when a config value is non-empty.
    /// Falls back to <see cref="UbuntuDeployDefaults"/> already on the model when a key is missing.
    /// </summary>
    public static void ApplyGitSource(IConfiguration configuration, UbuntuDeployFormModel model)
    {
        var s = configuration.GetSection(SectionKey);
        if (!string.IsNullOrWhiteSpace(s["GitRepo"]))
            model.GitRepo = s["GitRepo"]!.Trim();
        if (!string.IsNullOrWhiteSpace(s["Branch"]))
            model.Branch = s["Branch"]!.Trim();
        if (!string.IsNullOrWhiteSpace(s["DockerfilePath"]))
            model.DockerfilePath = s["DockerfilePath"]!.Trim();
        if (!string.IsNullOrWhiteSpace(s["DockerContext"]))
            model.DockerContext = s["DockerContext"]!.Trim();
    }
}
