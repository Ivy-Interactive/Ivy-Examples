using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Credentials;

public sealed class DeleteCredentialsCommand : AsyncCommand<DeleteCredentialsCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--credential-id <ID>")]
        [Description("The ID of the registry credentials to delete")]
        public required string CredentialId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        await client.DeleteAsync($"registry-credentials/{settings.CredentialId}");
        AnsiConsole.MarkupLine("[green]Credentials deleted.[/]");
        return 0;
    }
}
