using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Config;

/// <summary>ivy-examples config unset sliplane_api_key — remove a single key from ~/.ivy/config.json.</summary>
public sealed class ConfigUnsetCommand : Command<ConfigUnsetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Config key to remove (e.g. sliplane_api_key)")]
        public required string Key { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (ConfigStore.Remove(settings.Key))
            AnsiConsole.MarkupLine($"[green]Removed[/] [dim]{settings.Key}[/] from [dim]~/.ivy/config.json[/].");
        else
            AnsiConsole.MarkupLine($"[yellow]{settings.Key}[/] was not set — nothing to remove.");
        return 0;
    }
}
