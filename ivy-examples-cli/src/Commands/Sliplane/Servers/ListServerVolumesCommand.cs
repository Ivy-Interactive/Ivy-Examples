using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Servers;

public sealed class ListServerVolumesCommand : AsyncCommand<ListServerVolumesCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--server-id <ID>")]
        [Description("The ID of the server")]
        public required string ServerId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.GetAsync($"servers/{settings.ServerId}/volumes");
        YamlOutput.Write(result);
        return 0;
    }
}
