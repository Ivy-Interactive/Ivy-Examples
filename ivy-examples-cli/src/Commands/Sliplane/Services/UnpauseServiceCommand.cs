using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class UnpauseServiceCommand : AsyncCommand<UnpauseServiceCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project")]
        public required string ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("The ID of the service to unpause")]
        public required string ServiceId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        await client.PostAsync($"projects/{settings.ProjectId}/services/{settings.ServiceId}/unpause");
        AnsiConsole.MarkupLine("[green]Unpause request accepted.[/]");
        return 0;
    }
}
