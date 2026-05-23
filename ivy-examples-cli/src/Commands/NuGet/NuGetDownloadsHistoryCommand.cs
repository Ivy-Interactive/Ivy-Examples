using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.NuGet;

public sealed class NuGetDownloadsHistoryCommand : AsyncCommand<NuGetStatsSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NuGetStatsSettings settings)
    {
        var client = settings.CreateNuGetStatsClient();
        var doc = await client.GetAsync("downloads/history");
        YamlOutput.Write(doc);
        return 0;
    }
}
