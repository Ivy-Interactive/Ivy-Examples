using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class AddDomainCommand : AsyncCommand<AddDomainCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project")]
        public required string ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("The ID of the service")]
        public required string ServiceId { get; init; }

        [CommandOption("--domain <DOMAIN>")]
        [Description("The custom domain to add")]
        public required string Domain { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var result = await client.PostAsync(
            $"projects/{settings.ProjectId}/services/{settings.ServiceId}/domains",
            new { domain = settings.Domain });
        YamlOutput.Write(result);
        return 0;
    }
}
