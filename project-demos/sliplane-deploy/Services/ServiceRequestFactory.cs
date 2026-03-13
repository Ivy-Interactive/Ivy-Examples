namespace SliplaneDeploy.Services;

using SliplaneDeploy.Models;

internal static class ServiceRequestFactory
{
    public static CreateServiceRequest BuildCreateRequest(
        string name,
        string serverId,
        string gitRepo,
        string branch,
        string dockerfilePath,
        string dockerContext,
        bool autoDeploy,
        bool networkPublic,
        string networkProtocol,
        string? cmd,
        string? healthcheck,
        IReadOnlyCollection<EnvironmentVariable>? env,
        IReadOnlyCollection<(string VolumeId, string MountPath)>? volumeMounts)
    {
        var envList = env is { Count: > 0 } ? env.ToList() : null;
        var volumes = volumeMounts is { Count: > 0 }
            ? volumeMounts.Select(v => new ServiceVolumeMount(v.VolumeId, v.MountPath)).ToList()
            : null;

        return new CreateServiceRequest(
            Name: name.Trim(),
            ServerId: serverId,
            Network: new ServiceNetworkRequest(Public: networkPublic, Protocol: networkProtocol),
            Deployment: new RepositoryDeployment(
                Url: gitRepo.Trim(),
                Branch: string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim(),
                AutoDeploy: autoDeploy,
                DockerfilePath: string.IsNullOrWhiteSpace(dockerfilePath) ? "Dockerfile" : dockerfilePath.Trim(),
                DockerContext: string.IsNullOrWhiteSpace(dockerContext) ? "." : dockerContext.Trim()
            ),
            Cmd: string.IsNullOrWhiteSpace(cmd) ? null : cmd.Trim(),
            Healthcheck: string.IsNullOrWhiteSpace(healthcheck) ? null : healthcheck.Trim(),
            Env: envList,
            Volumes: volumes
        );
    }
}
