namespace Ivy.Cli.Infrastructure;

/// <summary>Executable name produced by NuGet PackAsTool (<c>dotnet tool install</c>).</summary>
internal static class CliBrand
{
    /// <summary>Mirrors <c>&lt;ToolCommandName&gt;</c> in <c>Ivy.Examples.Cli.csproj</c>; keep both in sync.</summary>
    internal const string ToolCommandName = "ivy-examples";
}
