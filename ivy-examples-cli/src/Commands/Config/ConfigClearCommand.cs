using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Config;

/// <summary>ivy-examples config clear — delete the entire ~/.ivy/config.json after confirmation.</summary>
public sealed class ConfigClearCommand : Command<ConfigClearCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (!AnsiConsole.Confirm("[red]Delete all saved config[/] in [dim]~/.ivy/config.json[/]?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
            return 0;
        }

        ConfigStore.Clear();
        AnsiConsole.MarkupLine("[green]Config cleared.[/] All saved keys have been removed.");
        return 0;
    }
}
