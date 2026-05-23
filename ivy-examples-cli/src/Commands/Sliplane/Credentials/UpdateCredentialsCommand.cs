using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Credentials;

public sealed class UpdateCredentialsCommand : AsyncCommand<UpdateCredentialsCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--credential-id <ID>")]
        [Description("The ID of the registry credentials to update")]
        public required string CredentialId { get; init; }

        [CommandOption("--name <NAME>")]
        [Description("The new name for the credentials")]
        public required string Name { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.PatchAsync(
            $"registry-credentials/{settings.CredentialId}", new { name = settings.Name });
        YamlOutput.Write(result);
        return 0;
    }
}
