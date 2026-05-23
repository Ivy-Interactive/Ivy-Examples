using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class CreateServiceCommand : AsyncCommand<CreateServiceCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project")]
        public required string ProjectId { get; init; }

        [CommandOption("--name <NAME>")]
        [Description("The name of the service")]
        public required string Name { get; init; }

        [CommandOption("--server-id <ID>")]
        [Description("The ID of the server to run the service on")]
        public required string ServerId { get; init; }

        [CommandOption("--image <URL>")]
        [Description("Container image URL (e.g. docker.io/library/nginx:latest)")]
        public string? Image { get; init; }

        [CommandOption("--repo <URL>")]
        [Description("Repository URL (e.g. https://github.com/user/repo)")]
        public string? Repo { get; init; }

        [CommandOption("--branch <BRANCH>")]
        [Description("Branch to deploy from (default: main)")]
        public string? Branch { get; init; }

        [CommandOption("--dockerfile <PATH>")]
        [Description("Path to Dockerfile")]
        public string? DockerfilePath { get; init; }

        [CommandOption("--docker-context <PATH>")]
        [Description("Docker build context (default: .)")]
        public string? DockerContext { get; init; }

        [CommandOption("--auto-deploy")]
        [Description("Auto-deploy on push")]
        public bool? AutoDeploy { get; init; }

        [CommandOption("--deploy-include-paths <PATTERN>")]
        [Description("Only deploy if changes in these paths (repeatable)")]
        public string[]? DeployIncludePaths { get; init; }

        [CommandOption("--deploy-ignore-paths <PATTERN>")]
        [Description("Skip deploy if changes only in these paths (repeatable)")]
        public string[]? DeployIgnorePaths { get; init; }

        [CommandOption("--registry-credential-id <ID>")]
        [Description("Registry credential ID for private images")]
        public string? RegistryCredentialId { get; init; }

        [CommandOption("--public")]
        [Description("Make the service publicly accessible")]
        public bool Public { get; init; }

        [CommandOption("--protocol <PROTOCOL>")]
        [Description("Network protocol: http, tcp, udp")]
        public string? Protocol { get; init; }

        [CommandOption("--healthcheck <PATH>")]
        [Description("Health check path (default: /)")]
        public string? Healthcheck { get; init; }

        [CommandOption("--cmd <CMD>")]
        [Description("Override Docker CMD")]
        public string? Cmd { get; init; }

        [CommandOption("--env <KEY=VALUE>")]
        [Description("Environment variable (repeatable)")]
        public string[]? Env { get; init; }

        [CommandOption("--secret-env <KEY=VALUE>")]
        [Description("Secret environment variable (repeatable)")]
        public string[]? SecretEnv { get; init; }

        [CommandOption("--volume <VOLUME>")]
        [Description("Volume mount as id:/path or name:/path (repeatable)")]
        public string[]? Volumes { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var body = BuildBody(settings);
        var result = await client.PostAsync($"projects/{settings.ProjectId}/services", body);
        YamlOutput.Write(result);
        return 0;
    }

    private static Dictionary<string, object> BuildBody(Settings s)
    {
        var body = new Dictionary<string, object>
        {
            ["name"]     = s.Name,
            ["serverId"] = s.ServerId
        };

        if (!string.IsNullOrEmpty(s.Image))
        {
            var dep = new Dictionary<string, object> { ["url"] = s.Image };
            if (!string.IsNullOrEmpty(s.RegistryCredentialId))
                dep["registryAuthenticationId"] = s.RegistryCredentialId;
            body["deployment"] = dep;
        }
        else if (!string.IsNullOrEmpty(s.Repo))
        {
            var dep = new Dictionary<string, object> { ["url"] = s.Repo };
            if (!string.IsNullOrEmpty(s.Branch))       dep["branch"]          = s.Branch;
            if (!string.IsNullOrEmpty(s.DockerfilePath)) dep["dockerfilePath"] = s.DockerfilePath;
            if (!string.IsNullOrEmpty(s.DockerContext))  dep["dockerContext"]  = s.DockerContext;
            if (s.AutoDeploy.HasValue)                   dep["autoDeploy"]     = s.AutoDeploy.Value;
            if (s.DeployIncludePaths is not null)         dep["includePaths"]   = s.DeployIncludePaths;
            if (s.DeployIgnorePaths is not null)          dep["ignorePaths"]    = s.DeployIgnorePaths;
            body["deployment"] = dep;
        }

        var network = new Dictionary<string, object> { ["public"] = s.Public };
        if (!string.IsNullOrEmpty(s.Protocol)) network["protocol"] = s.Protocol;
        body["network"] = network;

        if (!string.IsNullOrEmpty(s.Healthcheck)) body["healthcheck"] = s.Healthcheck;
        if (!string.IsNullOrEmpty(s.Cmd))          body["cmd"]        = s.Cmd;

        var envVars = new List<Dictionary<string, object>>();
        foreach (var e in s.Env ?? [])
        {
            var parts = e.Split('=', 2);
            envVars.Add(new() { ["key"] = parts[0], ["value"] = parts.Length > 1 ? parts[1] : "", ["secret"] = false });
        }
        foreach (var e in s.SecretEnv ?? [])
        {
            var parts = e.Split('=', 2);
            envVars.Add(new() { ["key"] = parts[0], ["value"] = parts.Length > 1 ? parts[1] : "", ["secret"] = true });
        }
        if (envVars.Count > 0) body["env"] = envVars;

        if (s.Volumes is not null)
        {
            var vols = new List<Dictionary<string, object>>();
            foreach (var v in s.Volumes)
            {
                var parts = v.Split(':', 2);
                var vol = new Dictionary<string, object> { ["mountPath"] = parts[1] };
                if (parts[0].StartsWith("volume_")) vol["id"]   = parts[0];
                else                                 vol["name"] = parts[0];
                vols.Add(vol);
            }
            body["volumes"] = vols;
        }

        return body;
    }
}
