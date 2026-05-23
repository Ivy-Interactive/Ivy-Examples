using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Projects;

public sealed class CreateProjectCommand : AsyncCommand<CreateProjectCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--name <NAME>")]
        [Description("The name of the project")]
        public required string Name { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.PostAsync("projects", new { name = settings.Name });
        YamlOutput.Write(result);
        return 0;
    }
}
