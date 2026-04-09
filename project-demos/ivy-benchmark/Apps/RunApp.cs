using System.Collections.Immutable;

namespace IvyBenchmark.Apps;

[App(icon: Icons.Play, title: "Run Benchmark")]
public class RunApp : ViewBase
{
    private static string DetectIvyVersion()
    {
        var ivyAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Ivy");
        return ivyAssembly?.GetName().Version?.ToString() ?? "unknown";
    }

    public override object? Build()
    {
        var factory = UseService<BenchmarkDbContextFactory>();
        var client = UseService<IClientProvider>();
        var queryService = UseService<IQueryService>();

        var ivyVersion = UseState(DetectIvyVersion);
        var baseUrl = UseState("http://localhost:5000");
        var healthPath = UseState("/health");
        var iterations = UseState(100);
        var environment = UseState("local");

        var isRunning = UseState(false);
        var logOutput = UseState("");
        var results = UseState(ImmutableList<BenchmarkScenarioResult>.Empty);
        var runPhase = UseState("");

        var refreshToken = UseRefreshToken();

        async Task RunBenchmark()
        {
            isRunning.Set(true);
            logOutput.Set("");
            results.Set(ImmutableList<BenchmarkScenarioResult>.Empty);

            var config = new BenchmarkConfig(
                BaseUrl: baseUrl.Value,
                HealthPath: healthPath.Value,
                LatencyIterations: iterations.Value);

            void Log(string msg)
            {
                logOutput.Set(logOutput.Value + $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
                refreshToken.Refresh();
            }

            try
            {
                runPhase.Set("startup");
                Log("Starting startup benchmark...");
                var startupResult = await BenchmarkRunnerService.MeasureStartupAsync(config, Log);
                results.Set(results.Value.Add(startupResult));
                Log(startupResult.Success
                    ? $"Startup: {startupResult.ValueMs:F0} ms"
                    : $"Startup FAILED: {startupResult.ErrorMessage}");

                runPhase.Set("latency");
                Log("Starting latency benchmark...");
                var latencyResult = await BenchmarkRunnerService.MeasureLatencyAsync(config, Log);
                results.Set(results.Value.Add(latencyResult));
                Log(latencyResult.Success
                    ? $"Latency: avg={latencyResult.ValueMs:F1}ms p95={latencyResult.P95Ms:F1}ms"
                    : $"Latency FAILED: {latencyResult.ErrorMessage}");

                runPhase.Set("saving");
                Log("Saving results to database...");
                await SaveRunAsync(factory, ivyVersion.Value, environment.Value, results.Value);
                Log("Saved successfully.");
                queryService.RevalidateByTag("benchmark-dashboard");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                client.Toast($"Benchmark failed: {ex.Message}");
            }
            finally
            {
                runPhase.Set("done");
                isRunning.Set(false);
            }
        }

        var envOptions = new[] { "local", "ci" }.ToOptions();

        var form = Layout.Vertical().Gap(4)
                   | Text.H3("Configuration")
                   | (Layout.Grid().Columns(2).Gap(4)
                      | ivyVersion.ToTextInput().Placeholder("1.2.34.0").WithField().Label("Ivy Version")
                      | environment.ToSelectInput(envOptions).WithField().Label("Environment"))
                   | (Layout.Grid().Columns(2).Gap(4)
                      | baseUrl.ToTextInput().Placeholder("http://localhost:5000").WithField().Label("Base URL")
                      | healthPath.ToTextInput().Placeholder("/health").WithField().Label("Health Endpoint"))
                   | iterations.ToNumberInput().WithField().Label("Latency Iterations")
                   | new Button("Run Benchmark")
                       .Primary()
                       .Icon(Icons.Play)
                       .Loading(isRunning.Value)
                       .Disabled(isRunning.Value)
                       .OnClick(async _ => await RunBenchmark());

        object progressSection = new Empty();
        if (!string.IsNullOrEmpty(logOutput.Value))
        {
            progressSection = new Card()
                                  .Title(isRunning.Value ? $"Running: {runPhase.Value}" : "Completed")
                              | logOutput.ToCodeInput()
                                  .Language(Languages.Text)
                                  .Height(Size.Units(50));
        }

        object resultsSection = new Empty();
        if (results.Value.Count > 0)
        {
            var resultCards = results.Value.Select(BuildResultCard).ToArray();
            resultsSection = Layout.Vertical().Gap(4)
                             | Text.H3("Results")
                             | (Layout.Grid().Columns(2).Gap(4) | resultCards);
        }

        return Layout.Vertical().Gap(6)
               | form
               | progressSection
               | resultsSection;
    }

    private static object BuildResultCard(BenchmarkScenarioResult r)
    {
        var icon = r.Success ? Icons.CircleCheck : Icons.CircleX;
        var card = new Card().Title(r.ScenarioLabel).Icon(icon);

        var details = Layout.Vertical().Gap(2);

        if (r.MetricKind == "startup")
        {
            details |= new Details([
                new Detail("Time", $"{r.ValueMs:F0} ms", false),
                new Detail("Status", r.Success ? "OK" : "Failed", false),
            ]);
        }
        else if (r.MetricKind == "latency")
        {
            details |= new Details([
                new Detail("Avg", $"{r.ValueMs:F1} ms", false),
                new Detail("P95", $"{r.P95Ms:F1} ms", false),
                new Detail("Min", $"{r.MinMs:F1} ms", false),
                new Detail("Max", $"{r.MaxMs:F1} ms", false),
                new Detail("Iterations", $"{r.Iterations}", false),
            ]);
        }

        if (r.ErrorMessage != null)
            details |= Text.Block(r.ErrorMessage).Color(Colors.Destructive);

        return card | details;
    }

    private static async Task SaveRunAsync(
        BenchmarkDbContextFactory factory,
        string ivyVersion,
        string environment,
        ImmutableList<BenchmarkScenarioResult> scenarioResults)
    {
        await using var db = factory.CreateDbContext();

        var run = new BenchmarkRunEntity
        {
            IvyVersion = ivyVersion,
            Environment = environment,
            RunnerName = System.Environment.MachineName,
            StartedAt = DateTime.UtcNow,
        };

        foreach (var r in scenarioResults)
        {
            run.Results.Add(new BenchmarkResultEntity
            {
                ScenarioKey = r.ScenarioKey,
                ScenarioLabel = r.ScenarioLabel,
                MetricKind = r.MetricKind,
                ValueMs = r.ValueMs,
                P95Ms = r.P95Ms,
                MinMs = r.MinMs,
                MaxMs = r.MaxMs,
                Iterations = r.Iterations,
                Success = r.Success,
                ErrorMessage = r.ErrorMessage,
            });
        }

        run.CompletedAt = DateTime.UtcNow;
        db.Runs.Add(run);
        await db.SaveChangesAsync();
    }
}
