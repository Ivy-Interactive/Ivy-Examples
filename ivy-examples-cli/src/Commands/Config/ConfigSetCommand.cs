using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Config;

/// <summary>ivy-examples config set sliplane_api_key sk-xxx — store a value in ~/.ivy/config.json.</summary>
public sealed class ConfigSetCommand : Command<ConfigSetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Config key to set (e.g. sliplane_api_key, tendril_base_url, tendril_api_key)")]
        public required string Key { get; init; }

        [CommandArgument(1, "<VALUE>")]
        [Description("Value to store")]
        public required string Value { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        ConfigStore.Set(settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Saved[/] [dim]{settings.Key}[/] to [dim]~/.ivy/config.json[/].");
        return 0;
    }
}
