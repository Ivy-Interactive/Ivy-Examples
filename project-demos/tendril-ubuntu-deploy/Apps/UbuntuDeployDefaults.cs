namespace TendrilUbuntuDeploy.Apps;

/// <summary>Default Git source for the Ubuntu Desktop Docker image.</summary>
public static class UbuntuDeployDefaults
{
    /// <summary>
    /// The repo that contains <c>docker/Dockerfile.ubuntu-desktop</c>.
    /// Change this to your fork if you customise the image.
    /// </summary>
    public const string DefaultRepoUrl = "https://github.com/Ivy-Interactive/Ivy-Examples";
    public const string DefaultBranch = "main";
    public const string DefaultDockerfilePath = "project-demos/tendril-ubuntu-deploy/docker/Dockerfile.ubuntu-desktop";
    public const string DefaultDockerContext = "project-demos/tendril-ubuntu-deploy/docker";
    public const string DefaultNoVncPort = "8080";
    public const string DefaultRdpUser = "developer";
    public const string DefaultHomeMount = "/home/developer";
}
