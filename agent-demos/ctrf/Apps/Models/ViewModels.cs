namespace CTRF.Apps.Models;

public record TestRowModel
{
    public string Status { get; init; } = "";
    public string Name { get; init; } = "";
    public string Suite { get; init; } = "";
    public string Duration { get; init; } = "";
    public string Flaky { get; init; } = "";
    public string Retries { get; init; } = "";
    public int Index { get; init; }
}

public record ChartSlice
{
    public string Status { get; init; } = "";
    public int Count { get; init; }
}
