using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Tendrils;

/// <summary>ivy-examples tendril servers — list Sliplane servers available for Tendril deployment.</summary>
public sealed class ListTendrilServersCommand : AsyncCommand<ListTendrilServersCommand.Settings>
{
    public sealed class Settings : TendrilApiSettings
    {
        [CommandOption("--sliplane-token <TOKEN>")]
        [Description("Sliplane API token (or set SLIPLANE_API_KEY env var)")]
        public string? SliplaneToken { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var sliplaneToken = ConfigStore.Resolve(
            flagValue: settings.SliplaneToken,
            envVar:    Environment.GetEnvironmentVariable("SLIPLANE_API_KEY"),
            configKey: "sliplane_api_key",
            label:     "Sliplane API key",
            hint:      "Find it at [link]https://sliplane.io[/] → Team Settings → API Tokens",
            isSecret:  true);

        var client = settings.CreateTendrilClient();
        var result = await client.GetAsync("api/v1/servers", sliplaneToken);
        YamlOutput.Write(result);
        return 0;
    }
}
