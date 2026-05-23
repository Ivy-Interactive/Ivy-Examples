using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Config;

/// <summary>ivy-examples config get sliplane_api_key — print a stored config value.</summary>
public sealed class ConfigGetCommand : Command<ConfigGetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Config key to read (e.g. sliplane_api_key, tendril_base_url)")]
        public required string Key { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var value = ConfigStore.Get(settings.Key);
        if (value is null)
            AnsiConsole.MarkupLine($"[yellow]{settings.Key}[/] is not set in [dim]~/.ivy/config.json[/].");
        else
            AnsiConsole.WriteLine(value);
        return 0;
    }
}
