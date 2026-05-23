using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Servers;

public sealed class DeleteServerCommand : AsyncCommand<DeleteServerCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--server-id <ID>")]
        [Description("The ID of the server to delete")]
        public required string ServerId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        await client.DeleteAsync($"servers/{settings.ServerId}");
        AnsiConsole.MarkupLine("[green]Server deleted.[/]");
        return 0;
    }
}
