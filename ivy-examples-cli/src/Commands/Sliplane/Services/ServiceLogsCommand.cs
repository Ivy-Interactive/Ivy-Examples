using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class ServiceLogsCommand : AsyncCommand<ServiceLogsCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project")]
        public required string ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("The ID of the service")]
        public required string ServiceId { get; init; }

        [CommandOption("--from <TIMESTAMP>")]
        [Description("From timestamp (Unix seconds)")]
        public long? From { get; init; }

        [CommandOption("--to <TIMESTAMP>")]
        [Description("To timestamp (Unix seconds)")]
        public long? To { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var parts = new List<string>();
        if (settings.From.HasValue) parts.Add($"from={settings.From}");
        if (settings.To.HasValue)   parts.Add($"to={settings.To}");
        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var result = await client.GetAsync(
            $"projects/{settings.ProjectId}/services/{settings.ServiceId}/logs{query}");
        YamlOutput.Write(result);
        return 0;
    }
}
