using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Projects;

public sealed class ListProjectsCommand : AsyncCommand<ApiSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ApiSettings settings)
    {
        var client = settings.CreateClient();
        var result = await client.GetAsync("projects");
        YamlOutput.Write(result);
        return 0;
    }
}
