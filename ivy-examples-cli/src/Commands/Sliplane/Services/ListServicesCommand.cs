using System.ComponentModel;
using System.Text.Json;
using Ivy.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Sliplane.Services;

public sealed class ListServicesCommand : AsyncCommand<ListServicesCommand.Settings>
{
    public sealed class Settings : ApiSettings
    {
        [CommandOption("--project-id <ID>")]
        [Description("The ID of the project (lists across all projects if omitted)")]
        public string? ProjectId { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var client = settings.CreateClient();

        if (settings.ProjectId is not null)
        {
            var result = await client.GetAsync($"projects/{settings.ProjectId}/services");
            YamlOutput.Write(result);
            return 0;
        }

        var projects = await client.GetAsync("projects");
        var allServices = new List<object?>();
        foreach (var project in projects.RootElement.EnumerateArray())
        {
            var pid = project.GetProperty("id").GetString();
            var services = await client.GetAsync($"projects/{pid}/services");
            foreach (var svc in services.RootElement.EnumerateArray())
                allServices.Add(JsonToObject(svc));
        }
        YamlOutput.WriteObject(allServices);
        return 0;
    }

    private static object? JsonToObject(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => JsonToObject(p.Value)),
        JsonValueKind.Array  => e.EnumerateArray().Select(JsonToObject).ToList(),
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
        JsonValueKind.True   => (object)true,
        JsonValueKind.False  => false,
        _                    => null
    };
}
