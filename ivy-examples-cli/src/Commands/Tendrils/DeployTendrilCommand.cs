using System.ComponentModel;
using Ivy.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ivy.Cli.Commands.Tendrils;

public sealed class DeployTendrilCommand : AsyncCommand<DeployTendrilCommand.Settings>
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
            hint:      "Find it at https://sliplane.io → Team Settings → API Tokens",
            isSecret:  true);

        var tendrilClient = settings.CreateTendrilClient();

        // ── 1. Select server ───────────────────────────────────────────────
        var serversDoc = await tendrilClient.GetAsync("api/v1/servers", sliplaneToken);
        var servers = serversDoc.RootElement.EnumerateArray()
            .Select(s => (Id: s.GetProperty("id").GetString()!, Name: s.GetProperty("name").GetString()!))
            .ToList();

        var serverName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Server:")
                .HighlightStyle("green")
                .PageSize(10)
                .AddChoices(servers.Select(s => s.Name)));
        var serverId = servers.First(s => s.Name == serverName).Id;
        AnsiConsole.MarkupLine($"Server: [green]{serverName}[/]");

        // ── 2. Select project ──────────────────────────────────────────────
        var projectsDoc = await tendrilClient.GetAsync("api/v1/projects", sliplaneToken);
        var projects = projectsDoc.RootElement.EnumerateArray()
            .Select(p => (Id: p.GetProperty("id").GetString()!, Name: p.GetProperty("name").GetString()!))
            .ToList();

        var projectName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Project:")
                .HighlightStyle("green")
                .PageSize(10)
                .AddChoices(projects.Select(p => p.Name)));
        var projectId = projects.First(p => p.Name == projectName).Id;
        AnsiConsole.MarkupLine($"Project: [green]{projectName}[/]");

        // ── 3. Service name ────────────────────────────────────────────────
        var serviceName = AnsiConsole.Prompt(
            new TextPrompt<string>("Service name:")
                .PromptStyle("green")
                .DefaultValue("ivy-tendril")
                .DefaultValueStyle(new Spectre.Console.Style(Spectre.Console.Color.Green)));

        // ── 4. Login credentials ───────────────────────────────────────────
        var username = AnsiConsole.Prompt(
            new TextPrompt<string>("Username:")
                .PromptStyle("green")
                .Validate(v => string.IsNullOrWhiteSpace(v)
                    ? ValidationResult.Error("[red]Cannot be empty.[/]")
                    : ValidationResult.Success()));

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password [dim](min 8 chars)[/]:")
                .PromptStyle("green")
                .Secret()
                .Validate(v => v.Length < 8
                    ? ValidationResult.Error("[red]Minimum 8 characters.[/]")
                    : ValidationResult.Success()));

        // ── 5. API keys ────────────────────────────────────────────────────
        var anthropicKey = PromptOptionalSecret("Anthropic API key [dim](optional)[/]:");
        var claudeToken  = PromptOptionalSecret("Claude OAuth token [dim](optional)[/]:");
        var githubToken  = PromptOptionalSecret("GitHub token [dim](optional)[/]:");
        var openAiKey    = PromptOptionalSecret("OpenAI API key [dim](optional)[/]:");
        var geminiKey    = PromptOptionalSecret("Gemini API key [dim](optional)[/]:");

        // ── 6. Volume ──────────────────────────────────────────────────────
        string? volumeId = null;
        if (Confirm("Attach a persistent volume?"))
        {
            volumeId = AnsiConsole.Prompt(
                new TextPrompt<string>("Volume ID:")
                    .PromptStyle("green"));
        }

        // ── 8. Confirm and deploy ──────────────────────────────────────────
        if (!Confirm("Deploy?"))
        {
            AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
            return 0;
        }

        var body = new Dictionary<string, object?>
        {
            ["sliplaneApiToken"]  = sliplaneToken,
            ["projectId"]         = projectId,
            ["serverId"]          = serverId,
            ["serviceName"]       = serviceName,
            ["basicAuthUsername"] = username,
            ["basicAuthPassword"] = password,
        };

        if (anthropicKey is not null) body["anthropicApiKey"]     = anthropicKey;
        if (claudeToken  is not null) body["claudeCodeOAuthToken"] = claudeToken;
        if (githubToken  is not null) body["gitHubToken"]          = githubToken;
        if (openAiKey    is not null) body["openAiApiKey"]         = openAiKey;
        if (geminiKey    is not null) body["geminiApiKey"]         = geminiKey;
        if (volumeId is not null) body["volumeId"] = volumeId;

        AnsiConsole.MarkupLine("Deploying...");
        var result = await tendrilClient.PostAsync("api/v1/tendrils", body);
        YamlOutput.Write(result);

        AnsiConsole.MarkupLine("[green]Done![/]");
        AnsiConsole.WriteLine();

        var nextCmd = $"{CliBrand.ToolCommandName} tendril status";
        AnsiConsole.Write(
            new Panel(
                    new Markup(
                        $"[grey]Run this until the instance is healthy (build takes a minute or two):[/]\n\n" +
                        $"[bold yellow]$[/][bold aqua] [/][bold underline aqua]{Markup.Escape(nextCmd)}[/]"))
                .Header($"[cyan]Command[/]")
                .Border(BoxBorder.Rounded)
                .Padding(1, 1));

        return 0;
    }

    private static string? PromptOptionalSecret(string label)
    {
        var value = AnsiConsole.Prompt(
            new TextPrompt<string>(label)
                .PromptStyle("green")
                .Secret()
                .AllowEmpty());
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool Confirm(string label, bool defaultValue = false)
    {
        var def = defaultValue ? "y" : "n";
        var answer = AnsiConsole.Prompt(
            new TextPrompt<string>($"{label} [green][[y/n]] ({def})[/]")
                .PromptStyle("green")
                .AllowEmpty()
                .Validate(v =>
                {
                    if (string.IsNullOrEmpty(v)) return ValidationResult.Success();
                    return v.ToLower() is "y" or "n" or "yes" or "no"
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Please enter y or n.[/]");
                }));

        return string.IsNullOrEmpty(answer) ? defaultValue : answer.ToLower() is "y" or "yes";
    }
}
