using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Servers;

public sealed class RescaleServerCommand : AsyncCommand<RescaleServerCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--server-id <ID>")]
        [Description("The ID of the server to rescale")]
        public required string ServerId { get; init; }

        [CommandOption("--instance-type <TYPE>")]
        [Description("New instance type (can only scale up)")]
        public required string InstanceType { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        await client.PostAsync($"servers/{settings.ServerId}", new { instanceType = settings.InstanceType });
        AnsiConsole.MarkupLine("[green]Rescale request accepted.[/]");
        return 0;
    }
}
