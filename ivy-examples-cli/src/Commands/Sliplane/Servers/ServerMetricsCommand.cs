using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Servers;

public sealed class ServerMetricsCommand : AsyncCommand<ServerMetricsCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--server-id <ID>")]
        [Description("The ID of the server")]
        public required string ServerId { get; init; }

        [CommandOption("--range <RANGE>")]
        [Description("Predefined time range: 10min, 1h, 24h, 7d (default: 1h)")]
        [DefaultValue("1h")]
        public string? Range { get; init; }

        [CommandOption("--from <TIMESTAMP>")]
        [Description("From timestamp (Unix seconds)")]
        public long? From { get; init; }

        [CommandOption("--to <TIMESTAMP>")]
        [Description("To timestamp (Unix seconds)")]
        public long? To { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(settings.Range)) parts.Add($"range={settings.Range}");
        if (settings.From.HasValue) parts.Add($"from={settings.From}");
        if (settings.To.HasValue) parts.Add($"to={settings.To}");
        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var result = await client.GetAsync($"servers/{settings.ServerId}/metrics{query}");
        YamlOutput.Write(result);
        return 0;
    }
}
