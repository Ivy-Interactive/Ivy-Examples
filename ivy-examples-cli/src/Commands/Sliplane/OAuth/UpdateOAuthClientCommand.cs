using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.OAuth;

public sealed class UpdateOAuthClientCommand : AsyncCommand<UpdateOAuthClientCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--client-id <ID>")]
        [Description("The OAuth client ID to update")]
        public required string ClientId { get; init; }

        [CommandOption("--name <NAME>")]
        [Description("Updated display name")]
        public string? Name { get; init; }

        [CommandOption("--image-url <URL>")]
        [Description("Updated image URL (empty string to clear)")]
        public string? ImageUrl { get; init; }

        [CommandOption("--redirect-uri <URI>")]
        [Description("Redirect URI (repeatable, replaces all)")]
        public string[]? RedirectUris { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(settings.Name))     body["name"]         = settings.Name;
        if (settings.ImageUrl is not null)             body["imageUrl"]     = settings.ImageUrl;
        if (settings.RedirectUris is not null)         body["redirectUris"] = settings.RedirectUris;
        var result = await client.PatchAsync($"oauth-clients/{settings.ClientId}", body);
        YamlOutput.Write(result);
        return 0;
    }
}
