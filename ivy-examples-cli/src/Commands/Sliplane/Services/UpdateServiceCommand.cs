using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class UpdateServiceCommand : AsyncCommand<UpdateServiceCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project")]
        public required string ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("The ID of the service")]
        public required string ServiceId { get; init; }

        [CommandOption("--name <NAME>")]
        [Description("New name for the service")]
        public string? Name { get; init; }

        [CommandOption("--image <URL>")]
        [Description("New container image URL")]
        public string? Image { get; init; }

        [CommandOption("--repo <URL>")]
        [Description("New repository URL")]
        public string? Repo { get; init; }

        [CommandOption("--branch <BRANCH>")]
        [Description("Branch to deploy from")]
        public string? Branch { get; init; }

        [CommandOption("--dockerfile <PATH>")]
        [Description("Path to Dockerfile")]
        public string? DockerfilePath { get; init; }

        [CommandOption("--docker-context <PATH>")]
        [Description("Docker build context")]
        public string? DockerContext { get; init; }

        [CommandOption("--auto-deploy")]
        [Description("Auto-deploy on push")]
        public bool? AutoDeploy { get; init; }

        [CommandOption("--deploy-include-paths <PATTERN>")]
        [Description("Only deploy if changes in these paths (repeatable, replaces all)")]
        public string[]? DeployIncludePaths { get; init; }

        [CommandOption("--deploy-ignore-paths <PATTERN>")]
        [Description("Skip deploy if changes only in these paths (repeatable, replaces all)")]
        public string[]? DeployIgnorePaths { get; init; }

        [CommandOption("--registry-credential-id <ID>")]
        [Description("Registry credential ID")]
        public string? RegistryCredentialId { get; init; }

        [CommandOption("--healthcheck <PATH>")]
        [Description("Health check path")]
        public string? Healthcheck { get; init; }

        [CommandOption("--cmd <CMD>")]
        [Description("Override Docker CMD")]
        public string? Cmd { get; init; }

        [CommandOption("--env <KEY=VALUE>")]
        [Description("Environment variable (repeatable, replaces all)")]
        public string[]? Env { get; init; }

        [CommandOption("--secret-env <KEY=VALUE>")]
        [Description("Secret environment variable (repeatable)")]
        public string[]? SecretEnv { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var body = await BuildBodyAsync(client, settings);
        var result = await client.PatchAsync($"projects/{settings.ProjectId}/services/{settings.ServiceId}", body);
        YamlOutput.Write(result);
        return 0;
    }

    private static async Task<Dictionary<string, object>> BuildBodyAsync(SliplaneClient client, Settings s)
    {
        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(s.Name))        body["name"]        = s.Name;
        if (!string.IsNullOrEmpty(s.Healthcheck)) body["healthcheck"] = s.Healthcheck;
        if (!string.IsNullOrEmpty(s.Cmd))         body["cmd"]         = s.Cmd;

        var dep = new Dictionary<string, object>();
        bool needsUrl = s.AutoDeploy.HasValue || s.DeployIncludePaths is not null || s.DeployIgnorePaths is not null;

        if (!string.IsNullOrEmpty(s.Image))
        {
            dep["url"] = s.Image;
            if (!string.IsNullOrEmpty(s.RegistryCredentialId)) dep["registryAuthenticationId"] = s.RegistryCredentialId;
        }
        else if (!string.IsNullOrEmpty(s.Repo))
        {
            dep["url"] = s.Repo;
            if (!string.IsNullOrEmpty(s.Branch))        dep["branch"]         = s.Branch;
            if (!string.IsNullOrEmpty(s.DockerfilePath)) dep["dockerfilePath"] = s.DockerfilePath;
            if (!string.IsNullOrEmpty(s.DockerContext))  dep["dockerContext"]  = s.DockerContext;
        }
        else if (needsUrl)
        {
            var current = await client.GetAsync($"projects/{s.ProjectId}/services/{s.ServiceId}");
            dep["url"] = current.RootElement.GetProperty("deployment").GetProperty("url").GetString()!;
        }

        if (s.AutoDeploy.HasValue)           dep["autoDeploy"]  = s.AutoDeploy.Value;
        if (s.DeployIncludePaths is not null) dep["includePaths"] = s.DeployIncludePaths;
        if (s.DeployIgnorePaths is not null)  dep["ignorePaths"]  = s.DeployIgnorePaths;

        if (dep.Count > 0) body["deployment"] = dep;

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

        return body;
    }
}
