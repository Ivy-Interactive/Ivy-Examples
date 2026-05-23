using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.NuGet;

public sealed class NuGetStarredCommand : AsyncCommand<NuGetStatsSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NuGetStatsSettings settings)
    {
        var client = settings.CreateNuGetStatsClient();
        var doc = await client.GetAsync("starred");
        YamlOutput.Write(doc);
        return 0;
    }
}
