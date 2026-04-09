namespace IvyBenchmark.Apps;

[App(icon: Icons.GitCompare, title: "Compare Versions")]
public class VersionCompareApp : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<BenchmarkDbContextFactory>();

        var versionA = UseState("");
        var versionB = UseState("");

        var versionsQuery = UseQuery<List<string>>(
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Runs
                    .Select(r => r.IvyVersion)
                    .Distinct()
                    .OrderByDescending(v => v)
                    .ToListAsync(ct);
            },
            tags: ["benchmark-dashboard"]);

        var compareQuery = UseQuery<List<CompareRow>?, (string, string)>(
            key: (versionA.Value, versionB.Value),
            fetcher: async (key, ct) =>
            {
                if (string.IsNullOrEmpty(key.Item1) || string.IsNullOrEmpty(key.Item2))
                    return null;
                return await LoadComparisonAsync(factory, key.Item1, key.Item2, ct);
            },
            tags: ["benchmark-dashboard"]);

        var versions = versionsQuery.Value ?? [];

        if (versionsQuery.Loading && versions.Count == 0)
            return new Skeleton().Height(Size.Units(40));

        if (versions.Count < 2)
            return Layout.Vertical().Height(Size.Full()).AlignContent(Align.Center)
                   | new Icon(Icons.GitCompare)
                   | Text.H3("Need at least 2 versions to compare")
                   | Text.Block("Run benchmarks on different Ivy versions first.").Muted();

        if (string.IsNullOrEmpty(versionA.Value) && versions.Count > 0) versionA.Set(versions[0]);
        if (string.IsNullOrEmpty(versionB.Value) && versions.Count > 1) versionB.Set(versions[1]);

        var versionOptions = versions.ToOptions();

        var selectors = Layout.Grid().Columns(2).Gap(4)
                        | versionA.ToSelectInput(versionOptions).WithField().Label("Baseline (A)")
                        | versionB.ToSelectInput(versionOptions).WithField().Label("Compare (B)");

        object table;
        if (compareQuery.Loading)
        {
            table = new Skeleton().Height(Size.Units(30));
        }
        else if (compareQuery.Value == null || compareQuery.Value.Count == 0)
        {
            table = Text.Muted("No common scenarios to compare.");
        }
        else
        {
            table = compareQuery.Value.AsQueryable()
                .ToDataTable()
                .Header(r => r.Scenario, "Scenario")
                .Header(r => r.MetricKind, "Type")
                .Header(r => r.ValueA, "A (ms)")
                .Header(r => r.ValueB, "B (ms)")
                .Header(r => r.DeltaMs, "Delta (ms)")
                .Header(r => r.DeltaPercent, "Delta (%)");
        }

        return Layout.Vertical().Gap(6)
               | selectors
               | table;
    }

    private static async Task<List<CompareRow>> LoadComparisonAsync(
        BenchmarkDbContextFactory factory,
        string versionA,
        string versionB,
        CancellationToken ct)
    {
        await using var db = factory.CreateDbContext();

        var runsA = await db.Runs
            .Where(r => r.IvyVersion == versionA)
            .Include(r => r.Results)
            .OrderByDescending(r => r.StartedAt)
            .Take(1)
            .ToListAsync(ct);

        var runsB = await db.Runs
            .Where(r => r.IvyVersion == versionB)
            .Include(r => r.Results)
            .OrderByDescending(r => r.StartedAt)
            .Take(1)
            .ToListAsync(ct);

        var resultsA = runsA.SelectMany(r => r.Results).ToDictionary(r => r.ScenarioKey);
        var resultsB = runsB.SelectMany(r => r.Results).ToDictionary(r => r.ScenarioKey);

        var allKeys = resultsA.Keys.Union(resultsB.Keys).OrderBy(k => k).ToList();

        return allKeys.Select(key =>
        {
            var a = resultsA.GetValueOrDefault(key);
            var b = resultsB.GetValueOrDefault(key);
            var valA = a?.ValueMs ?? 0;
            var valB = b?.ValueMs ?? 0;
            var delta = valB - valA;
            var pct = valA != 0 ? (delta / valA) * 100 : 0;

            return new CompareRow(
                a?.ScenarioLabel ?? b?.ScenarioLabel ?? key,
                a?.MetricKind ?? b?.MetricKind ?? "",
                Math.Round(valA, 1),
                Math.Round(valB, 1),
                Math.Round(delta, 1),
                Math.Round(pct, 1));
        }).ToList();
    }
}

public record CompareRow(
    string Scenario,
    string MetricKind,
    double ValueA,
    double ValueB,
    double DeltaMs,
    double DeltaPercent);
