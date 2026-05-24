using Spectre.Console;

namespace Ivy.Cli.Infrastructure;

/// <summary>CLI product labels mapped to Ivy Insights <c>?package=</c> query values.</summary>
public readonly record struct NuGetProduct(string DisplayName, string PackageQuery)
{
    public static NuGetProduct IvyFramework { get; } = new("Ivy-Framework", "ivy");
    public static NuGetProduct IvyTendril { get; } = new("Ivy-Tendril", "tendril");

    public static IReadOnlyList<NuGetProduct> All { get; } = [IvyFramework, IvyTendril];

    public static NuGetProduct Resolve(string? productFlag)
    {
        if (!string.IsNullOrWhiteSpace(productFlag))
        {
            if (TryParse(productFlag, out var parsed))
                return parsed;

            throw new InvalidOperationException(
                $"Unknown product '{productFlag}'. Use Ivy-Framework or Ivy-Tendril.");
        }

        return Prompt();
    }

    public static NuGetProduct Prompt() =>
        AnsiConsole.Prompt(
            new SelectionPrompt<NuGetProduct>()
                .Title("Select [green]product[/]")
                .UseConverter(p => p.DisplayName)
                .AddChoices(All));

    public static bool TryParse(string value, out NuGetProduct product)
    {
        var key = value.Trim().ToLowerInvariant().Replace('_', '-');
        switch (key)
        {
            case "ivy-framework" or "ivy" or "framework":
                product = IvyFramework;
                return true;
            case "ivy-tendril" or "tendril":
                product = IvyTendril;
                return true;
        }

        foreach (var candidate in All)
        {
            if (string.Equals(value, candidate.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                product = candidate;
                return true;
            }
        }

        product = IvyFramework;
        return false;
    }
}
