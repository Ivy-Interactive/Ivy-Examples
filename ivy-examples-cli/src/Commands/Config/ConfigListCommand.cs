using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Config;

/// <summary>ivy-examples config list — show all stored config keys (values masked for secrets).</summary>
public sealed class ConfigListCommand : Command<ConfigListCommand.Settings>
{
    public sealed class Settings : CommandSettings { }
    private static readonly HashSet<string> SecretKeys =
    [
        "sliplane_api_key",
        "tendril_api_key"
    ];

    public override int Execute(CommandContext context, Settings settings)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Key")
            .AddColumn("Value");

        var keys = new[]
        {
            "sliplane_api_key",
            "sliplane_org_id",
            "tendril_base_url",
            "tendril_api_key"
        };

        bool anySet = false;
        foreach (var key in keys)
        {
            var raw = ConfigStore.Get(key);
            if (raw is null) continue;
            anySet = true;
            var display = SecretKeys.Contains(key)
                ? "[dim]" + raw[..Math.Min(8, raw.Length)] + "..." + "[/]"
                : raw;
            table.AddRow($"[yellow]{key}[/]", display);
        }

        if (!anySet)
        {
            AnsiConsole.MarkupLine("[dim]No values saved yet in ~/.ivy/config.json[/]");
            AnsiConsole.MarkupLine($"Run any command and answer [green]y[/] when asked to save, or use [dim]{CliBrand.ToolCommandName} config set <key> <value>[/].");
            return 0;
        }

        AnsiConsole.MarkupLine("[dim]~/.ivy/config.json[/]");
        AnsiConsole.Write(table);
        return 0;
    }
}
