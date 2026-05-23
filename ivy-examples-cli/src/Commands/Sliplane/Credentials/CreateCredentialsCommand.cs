using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Credentials;

public sealed class CreateCredentialsCommand : AsyncCommand<CreateCredentialsCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--name <NAME>")]
        [Description("A name to identify these credentials")]
        public required string Name { get; init; }

        [CommandOption("--type <TYPE>")]
        [Description("Registry type: ghcr, dockerhub, dhi, generic")]
        public required string Type { get; init; }

        [CommandOption("--username <USERNAME>")]
        [Description("Registry username")]
        public required string Username { get; init; }

        [CommandOption("--token <TOKEN>")]
        [Description("Registry authentication token")]
        public required string Token { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.PostAsync("registry-credentials", new
        {
            name     = settings.Name,
            type     = settings.Type,
            username = settings.Username,
            token    = settings.Token
        });
        YamlOutput.Write(result);
        return 0;
    }
}
