using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Servers;

public sealed class CreateServerVolumeCommand : AsyncCommand<CreateServerVolumeCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--server-id <ID>")]
        [Description("The ID of the server")]
        public required string ServerId { get; init; }

        [CommandOption("--name <NAME>")]
        [Description("The name of the volume")]
        public required string Name { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.PostAsync($"servers/{settings.ServerId}/volumes", new { name = settings.Name });
        YamlOutput.Write(result);
        return 0;
    }
}
