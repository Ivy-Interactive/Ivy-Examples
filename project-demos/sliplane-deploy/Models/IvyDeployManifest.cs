namespace SliplaneDeploy.Models;

/// <summary>
/// Parsed <c>ivy-deploy.yaml</c> (apiVersion: <c>ivy-deploy/v1</c>).
///
/// Template grammar:
///   <list type="bullet">
///     <item><c>{parentServiceName}</c> — early-bound literal substitution applied to
///       <see cref="ServiceName"/>, <see cref="IvyDeployChildService.ServiceName"/>,
///       <see cref="IvyDeployVolumeSpec.VolumeName"/>, and env default/value strings.</item>
///     <item><c>{{childName:host}}</c> — late-bound reference to the child service's
///       <c>network.internalDomain</c>, resolved after the child is created.</item>
///     <item><c>{{childName:env:KEY}}</c> — late-bound reference to the value passed as
///       environment variable <c>KEY</c> when creating <c>childName</c>.</item>
///   </list>
/// Early-bound substitution runs first, so <c>{{{parentServiceName}-db:host}}</c> resolves
/// <c>{parentServiceName}</c> inside the reference before child lookup.
/// </summary>
public record IvyDeployManifest(
    string ApiVersion,
    string ServiceName,
    string? Title,
    string? GithubRepo,
    string? Branch,
    string? DockerfilePath,
    int? Port,
    IvyDeployHealthCheck? HealthCheck,
    List<IvyDeployEnvVar> Env,
    List<IvyDeployVolumeSpec> Volumes,
    List<IvyDeployChildService> ChildServices,
    IvyDeployRules? DeployRules
);

public record IvyDeployHealthCheck(
    string? Path,
    int? IntervalSeconds,
    int? TimeoutSeconds,
    int? StartPeriodSeconds
);

/// <summary>
/// Environment variable declaration. Exactly one of <see cref="Value"/>, <see cref="Default"/>,
/// or <see cref="Generate"/> should be supplied.
/// <see cref="Generate"/> currently supports <c>random:N</c> (N characters, URL-safe).
/// </summary>
public record IvyDeployEnvVar(
    string Name,
    string? Value = null,
    string? Default = null,
    string? Generate = null,
    bool Secret = false
);

public record IvyDeployVolumeSpec(
    string VolumeName,
    string MountPath,
    string? Size
);

/// <summary>
/// Child service. Either <see cref="ImageUrl"/> (container image) or <see cref="GithubRepo"/> must be set.
/// </summary>
public record IvyDeployChildService(
    string ServiceName,
    string? ImageUrl,
    string? GithubRepo,
    string? Branch,
    string? DockerfilePath,
    string? Description,
    List<IvyDeployEnvVar> Env,
    List<IvyDeployVolumeSpec> Volumes
);

public record IvyDeployRules(
    bool AutoDeploy,
    List<string>? IgnorePaths
);
