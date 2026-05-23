using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class RemoveDomainCommand : AsyncCommand<RemoveDomainCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project")]
        public required string ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("The ID of the service")]
        public required string ServiceId { get; init; }

        [CommandOption("--domain-id <ID>")]
        [Description("The ID of the custom domain to remove")]
        public required string DomainId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        await client.DeleteAsync(
            $"projects/{settings.ProjectId}/services/{settings.ServiceId}/domains/{settings.DomainId}");
        AnsiConsole.MarkupLine("[green]Domain removed.[/]");
        return 0;
    }
}
