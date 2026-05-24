using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.NuGet;

public sealed class NuGetDownloadsHistoryCommand : AsyncCommand<NuGetStatsSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NuGetStatsSettings settings)
    {
        var doc = await settings.FetchDownloadsHistoryAsync();
        YamlOutput.Write(doc);
        return 0;
    }
}
