using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Projects;

public sealed class DeleteProjectCommand : AsyncCommand<DeleteProjectCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project to delete")]
        public required string ProjectId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        await client.DeleteAsync($"projects/{settings.ProjectId}");
        AnsiConsole.MarkupLine("[green]Project deleted.[/]");
        return 0;
    }
}
