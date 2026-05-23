using System.Text.Json;
using Spectre.Console;

namespace Ivy.Cli.Infrastructure;

/// <summary>
/// Persists CLI configuration to ~/.ivy/config.json.
/// Keys stored: sliplane_api_key, sliplane_org_id, tendril_base_url, tendril_api_key.
/// </summary>
public static class ConfigStore
{
    private static readonly string ConfigDir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ivy");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── Public API ────────────────────────────────────────────────────────

    public static string? Get(string key)
    {
        var config = Load();
        return config.TryGetValue(key, out var val) ? val : null;
    }

    public static void Set(string key, string value)
    {
        var config = Load();
        config[key] = value;
        Save(config);
    }

    /// <summary>Removes a single key. Returns true if it existed.</summary>
    public static bool Remove(string key)
    {
        var config = Load();
        if (!config.Remove(key)) return false;
        Save(config);
        return true;
    }

    /// <summary>Deletes the entire config file.</summary>
    public static void Clear()
    {
        if (File.Exists(ConfigFile))
            File.Delete(ConfigFile);
    }

    /// <summary>
    /// Resolves a value using priority: flag → env var → config file → interactive prompt.
    /// If prompted, asks the user whether to save the value for future runs.
    /// </summary>
    public static string Resolve(
        string?  flagValue,
        string?  envVar,
        string   configKey,
        string   label,
        string   hint,
        bool     isSecret = true)
    {
        // 1. Flag
        if (!string.IsNullOrWhiteSpace(flagValue)) return flagValue!;

        // 2. Env var
        if (!string.IsNullOrWhiteSpace(envVar)) return envVar!;

        // 3. Config file
        var stored = Get(configKey);
        if (!string.IsNullOrWhiteSpace(stored)) return stored!;

        // 4. Interactive prompt
        AnsiConsole.MarkupLine($"[yellow]{label}[/] not set.");
        AnsiConsole.MarkupLine($"  {hint}");

        var prompt = new TextPrompt<string>($"Enter {label}:")
            .PromptStyle("green")
            .Validate(v => string.IsNullOrWhiteSpace(v)
                ? ValidationResult.Error("[red]Cannot be empty.[/]")
                : ValidationResult.Success());

        if (isSecret) prompt.Secret();

        var value = AnsiConsole.Prompt(prompt);

        // Ask whether to save
        var save = AnsiConsole.Confirm($"Save {label} to [dim]~/.ivy/config.json[/] for future runs?");
        if (save)
        {
            Set(configKey, value);
            AnsiConsole.MarkupLine($"[green]Saved.[/] You can also run [dim]{CliBrand.ToolCommandName} config set {configKey} <value>[/] anytime.");
        }

        return value;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static Dictionary<string, string> Load()
    {
        if (!File.Exists(ConfigFile))
            return new Dictionary<string, string>();

        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static void Save(Dictionary<string, string> config)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config, JsonOptions));
    }
}
