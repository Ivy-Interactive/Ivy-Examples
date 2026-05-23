using System.ComponentModel;
using System.Text.Json;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Tendrils;

/// <summary>
/// ivy-examples tendril status — interactively pick a Tendril service and show its status.
/// If --project-id and --service-id are provided, skips the interactive prompt.
/// </summary>
public sealed class GetTendrilStatusCommand : AsyncCommand<GetTendrilStatusCommand.Settings>
{
    public sealed class Settings : TendrilApiSettings
    {
        [CommandOption("--sliplane-token <TOKEN>")]
        [Description("Sliplane API token (or set SLIPLANE_API_KEY env var)")]
        public string? SliplaneToken { get; init; }

        [CommandOption("--project-id <ID>")]
        [Description("Sliplane project ID (optional — skips interactive picker if provided with --service-id)")]
        public string? ProjectId { get; init; }

        [CommandOption("--service-id <ID>")]
        [Description("Sliplane service ID (optional — skips interactive picker if provided with --project-id)")]
        public string? ServiceId { get; init; }
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

        var tendrilClient = settings.CreateTendrilClient();

        string projectId;
        string serviceId;

        // If both IDs provided — use them directly
        if (!string.IsNullOrEmpty(settings.ProjectId) && !string.IsNullOrEmpty(settings.ServiceId))
        {
            projectId = settings.ProjectId;
            serviceId = settings.ServiceId;
        }
        else
        {
            // Load all services from all Tendril projects and let user pick
            AnsiConsole.MarkupLine("[dim]Loading Tendril services...[/]");

            var projectsDoc = await tendrilClient.GetAsync("api/v1/projects", sliplaneToken);
            var projects = projectsDoc.RootElement.EnumerateArray().ToList();

            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No projects found.[/]");
                return 1;
            }

            // Fetch services for each project in parallel
            var sliplaneClient = new SliplaneClient(sliplaneToken);
            var allServices = new List<(string ProjectId, string ProjectName, string ServiceId, string ServiceName, string Status, DateTimeOffset UpdatedAt)>();

            foreach (var project in projects)
            {
                var pid  = project.GetProperty("id").GetString()!;
                var pname = project.GetProperty("name").GetString()!;

                try
                {
                    var servicesDoc = await sliplaneClient.GetAsync($"projects/{pid}/services");
                    foreach (var svc in servicesDoc.RootElement.EnumerateArray())
                    {
                        var sid = svc.GetProperty("id").GetString()!;
                        var sname = svc.GetProperty("name").GetString()!;
                        var status = svc.TryGetProperty("status", out var st) ? st.GetString() ?? "unknown" : "unknown";
                        var updatedAt = ParseServiceUpdatedAt(svc);
                        allServices.Add((pid, pname, sid, sname, status, updatedAt));
                    }
                }
                catch { /* skip projects we can't access */ }
            }

            if (allServices.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No services found across Tendril projects.[/]");
                return 1;
            }

            // Show most recently updated services first.
            allServices = allServices
                .OrderByDescending(s => s.UpdatedAt)
                .ThenBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Interactive picker
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]Tendril service[/] to check status:")
                    .PageSize(12)
                    .AddChoices(allServices.Select(s =>
                        $"{s.ServiceName}  [dim]({s.ProjectName})[/]  [{StatusColor(s.Status)}]{s.Status}[/]")));

            var index = allServices.FindIndex(s =>
                choice.StartsWith(s.ServiceName + "  "));

            if (index < 0)
            {
                AnsiConsole.MarkupLine("[red]Could not resolve selection.[/]");
                return 1;
            }

            projectId = allServices[index].ProjectId;
            serviceId = allServices[index].ServiceId;
        }

        // Fetch status from tendril-deploy API
        using var http = new System.Net.Http.HttpClient();
        var baseUrl = settings.TendrilUrl
            ?? Environment.GetEnvironmentVariable("TENDRIL_BASE_URL")
            ?? ConfigStore.Get("tendril_base_url")
            ?? "https://ivy-tendril-deployment.sliplane.app";

        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        var tendrilApiKey = settings.TendrilApiKey
            ?? Environment.GetEnvironmentVariable("TENDRIL_API_KEY")
            ?? ConfigStore.Get("tendril_api_key");

        if (!string.IsNullOrEmpty(tendrilApiKey))
            http.DefaultRequestHeaders.Add("X-Api-Key", tendrilApiKey);
        http.DefaultRequestHeaders.Add("X-Sliplane-Token", sliplaneToken);

        var response = await http.GetAsync($"api/v1/tendrils/{projectId}/{serviceId}");
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new System.Net.Http.HttpRequestException($"HTTP {(int)response.StatusCode}: {err}");
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        YamlOutput.Write(doc);
        return 0;
    }

    private static string StatusColor(string status) => status.ToLower() switch
    {
        "running"  => "green",
        "building" => "yellow",
        "stopped"  => "red",
        "error"    => "red",
        _          => "dim"
    };

    private static DateTimeOffset ParseServiceUpdatedAt(JsonElement service)
    {
        // Sliplane API fields may vary by version/casing.
        if (TryParseDate(service, "updated_at", out var updatedAt) ||
            TryParseDate(service, "updatedAt", out updatedAt) ||
            TryParseDate(service, "created_at", out updatedAt) ||
            TryParseDate(service, "createdAt", out updatedAt))
        {
            return updatedAt;
        }

        return DateTimeOffset.MinValue;
    }

    private static bool TryParseDate(JsonElement obj, string property, out DateTimeOffset value)
    {
        value = default;
        if (!obj.TryGetProperty(property, out var raw))
            return false;

        return raw.ValueKind == JsonValueKind.String &&
               DateTimeOffset.TryParse(raw.GetString(), out value);
    }
}
