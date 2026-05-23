using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Servers;

public sealed class CreateServerCommand : AsyncCommand<CreateServerCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--name <NAME>")]
        [Description("The name of the server")]
        public required string Name { get; init; }

        [CommandOption("--instance-type <TYPE>")]
        [Description("Instance type: base, medium, large, x-large, xx-large, dedicated-base, ...")]
        public required string InstanceType { get; init; }

        [CommandOption("--location <LOCATION>")]
        [Description("Location: sin, fsn, nbg, ash, hel, hil")]
        public required string Location { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.PostAsync("servers", new
        {
            name = settings.Name,
            instanceType = settings.InstanceType,
            location = settings.Location
        });
        YamlOutput.Write(result);
        return 0;
    }
}
