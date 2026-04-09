namespace IvyBenchmark.Apps;

[App(icon: Icons.LayoutDashboard, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<BenchmarkDbContextFactory>();
        var navigation = Context.UseNavigation();

        var dashQuery = UseQuery<DashboardModel?>(
            fetcher: async ct => await LoadDashboardAsync(factory, ct),
            options: new QueryOptions { KeepPrevious = true },
            tags: ["benchmark-dashboard"]);

        if (dashQuery.Loading && dashQuery.Value == null)
            return Layout.Vertical().Height(Size.Full()).AlignContent(Align.Center)
                   | new Skeleton().Height(Size.Units(40));

        var model = dashQuery.Value;
        if (model == null || model.Versions.Count == 0)
            return Layout.Vertical().Height(Size.Full()).AlignContent(Align.Center)
                   | new Icon(Icons.LayoutDashboard)
                   | Text.H3("No benchmark data yet")
                   | Text.Block("Run a benchmark to see dashboard statistics.").Muted()
                   | new Button("Run Benchmark", onClick: _ => navigation.Navigate(typeof(RunApp)))
                       .Primary().Icon(Icons.Play);

        // --- KPI Cards ---
        var latestStartup = model.LatestStartup;
        var latestLatency = model.LatestLatency;
        var prevStartup = model.PreviousStartup;
        var prevLatency = model.PreviousLatency;

        var kpiCards = Layout.Grid().Columns(3).Gap(4);

        kpiCards |= BuildKpiCard("Startup",
            latestStartup != null ? $"{latestStartup.ValueMs:F0} ms" : "—",
            latestStartup != null && prevStartup != null
                ? FormatDelta(latestStartup.ValueMs - prevStartup.ValueMs, "ms", higherIsBetter: false)
                : null,
            Icons.Zap);

        kpiCards |= BuildKpiCard("Avg Latency",
            latestLatency != null ? $"{latestLatency.ValueMs:F1} ms" : "—",
            latestLatency != null && prevLatency != null
                ? FormatDelta(latestLatency.ValueMs - prevLatency.ValueMs, "ms", higherIsBetter: false)
                : null,
            Icons.Timer);

        kpiCards |= BuildKpiCard("P95 Latency",
            latestLatency?.P95Ms != null ? $"{latestLatency.P95Ms:F1} ms" : "—",
            latestLatency?.P95Ms != null && prevLatency?.P95Ms != null
                ? FormatDelta(latestLatency.P95Ms.Value - prevLatency.P95Ms.Value, "ms", higherIsBetter: false)
                : null,
            Icons.TrendingUp);

        // --- Charts ---
        var startupChart = new StartupChartView(model.StartupByVersion);
        var latencyChart = new LatencyChartView(model.LatencyByVersion);

        // --- Runs table ---
        var runsTable = model.RecentRuns.AsQueryable()
            .ToDataTable()
            .Header(r => r.IvyVersion, "Version")
            .Header(r => r.Environment, "Env")
            .Header(r => r.StartupMs, "Startup (ms)")
            .Header(r => r.AvgLatencyMs, "Avg Latency (ms)")
            .Header(r => r.P95LatencyMs, "P95 (ms)")
            .Header(r => r.StartedAt, "Date");

        return Layout.Vertical().Gap(6)
               | kpiCards
               | (Layout.Grid().Columns(2).Gap(4)
                  | startupChart
                  | latencyChart)
               | new Card().Title("Recent Runs") | runsTable;
    }

    private static object BuildKpiCard(string title, string value, object? delta, Icons icon)
    {
        var body = Layout.Vertical().Gap(1)
                   | Text.H2(value)
                   | (delta ?? Text.Muted("no baseline"));

        return new Card().Title(title).Icon(icon) | body;
    }

    private static object FormatDelta(double delta, string unit, bool higherIsBetter)
    {
        var improved = higherIsBetter ? delta > 0 : delta < 0;
        var sign = delta > 0 ? "+" : "";
        var text = $"{sign}{delta:F1} {unit}";
        return improved
            ? Text.Block(text).Color(Colors.Success)
            : delta == 0
                ? Text.Muted(text)
                : Text.Block(text).Color(Colors.Destructive);
    }

    private static async Task<DashboardModel?> LoadDashboardAsync(
        BenchmarkDbContextFactory factory,
        CancellationToken ct)
    {
        await using var db = factory.CreateDbContext();

        var runs = await db.Runs
            .Include(r => r.Results)
            .OrderByDescending(r => r.StartedAt)
            .Take(100)
            .ToListAsync(ct);

        if (runs.Count == 0) return null;

        var versions = runs
            .Select(r => r.IvyVersion)
            .Distinct()
            .OrderByDescending(v => v)
            .ToList();

        BenchmarkResultEntity? FindResult(string version, string scenarioKey)
        {
            return runs
                .Where(r => r.IvyVersion == version)
                .SelectMany(r => r.Results)
                .FirstOrDefault(r => r.ScenarioKey == scenarioKey && r.Success);
        }

        var latestVersion = versions.First();
        var previousVersion = versions.Count > 1 ? versions[1] : null;

        var latestStartup = FindResult(latestVersion, "startup_health");
        var latestLatency = FindResult(latestVersion, "latency_get_health");
        var prevStartup = previousVersion != null ? FindResult(previousVersion, "startup_health") : null;
        var prevLatency = previousVersion != null ? FindResult(previousVersion, "latency_get_health") : null;

        // Startup by version: latest successful per version
        var startupByVersion = versions
            .Select(v => {
                var r = FindResult(v, "startup_health");
                return r != null ? new VersionMetricRow(v, r.ValueMs) : null;
            })
            .Where(x => x != null)
            .Cast<VersionMetricRow>()
            .OrderBy(x => x.Version)
            .ToList();

        // Latency by version: avg + p95
        var latencyByVersion = versions
            .Select(v => {
                var r = FindResult(v, "latency_get_health");
                return r != null ? new VersionLatencyRow(v, r.ValueMs, r.P95Ms ?? 0) : null;
            })
            .Where(x => x != null)
            .Cast<VersionLatencyRow>()
            .OrderBy(x => x.Version)
            .ToList();

        // Recent runs for table
        var recentRuns = runs.Take(20).Select(r =>
        {
            var startup = r.Results.FirstOrDefault(x => x.ScenarioKey == "startup_health");
            var latency = r.Results.FirstOrDefault(x => x.ScenarioKey == "latency_get_health");
            return new RunSummaryRow(
                r.IvyVersion,
                r.Environment,
                startup?.ValueMs ?? 0,
                latency?.ValueMs ?? 0,
                latency?.P95Ms ?? 0,
                r.StartedAt);
        }).ToList();

        return new DashboardModel(
            versions,
            latestStartup,
            latestLatency,
            prevStartup,
            prevLatency,
            startupByVersion,
            latencyByVersion,
            recentRuns);
    }
}

public record DashboardModel(
    List<string> Versions,
    BenchmarkResultEntity? LatestStartup,
    BenchmarkResultEntity? LatestLatency,
    BenchmarkResultEntity? PreviousStartup,
    BenchmarkResultEntity? PreviousLatency,
    List<VersionMetricRow> StartupByVersion,
    List<VersionLatencyRow> LatencyByVersion,
    List<RunSummaryRow> RecentRuns);

public record VersionMetricRow(string Version, double ValueMs);

public record VersionLatencyRow(string Version, double AvgMs, double P95Ms);

public record RunSummaryRow(
    string IvyVersion,
    string Environment,
    double StartupMs,
    double AvgLatencyMs,
    double P95LatencyMs,
    DateTime StartedAt);
