using System.Diagnostics;

namespace IvyBenchmark.Services;

public record BenchmarkScenarioResult(
    string ScenarioKey,
    string ScenarioLabel,
    string MetricKind,
    double ValueMs,
    double? P95Ms,
    double? MinMs,
    double? MaxMs,
    int Iterations,
    bool Success,
    string? ErrorMessage);

public record BenchmarkConfig(
    string BaseUrl = "http://localhost:5000",
    string HealthPath = "/health",
    int LatencyIterations = 100,
    int WarmUpRequests = 10,
    int StartupTimeoutMs = 30_000,
    int PollIntervalMs = 200);

public static class BenchmarkRunnerService
{
    public static async Task<BenchmarkScenarioResult> MeasureStartupAsync(
        BenchmarkConfig config,
        Action<string>? onStatus = null,
        CancellationToken ct = default)
    {
        var url = $"{config.BaseUrl.TrimEnd('/')}{config.HealthPath}";
        onStatus?.Invoke($"Polling {url} ...");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < config.StartupTimeoutMs)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var resp = await http.GetAsync(url, ct);
                if (resp.IsSuccessStatusCode)
                {
                    var elapsed = sw.Elapsed.TotalMilliseconds;
                    onStatus?.Invoke($"Server ready in {elapsed:F0} ms");
                    return new BenchmarkScenarioResult(
                        ScenarioKey: "startup_health",
                        ScenarioLabel: $"Startup (GET {config.HealthPath})",
                        MetricKind: "startup",
                        ValueMs: Math.Round(elapsed, 1),
                        P95Ms: null,
                        MinMs: null,
                        MaxMs: null,
                        Iterations: 1,
                        Success: true,
                        ErrorMessage: null);
                }
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // Server not ready yet
            }

            await Task.Delay(config.PollIntervalMs, ct);
        }

        return new BenchmarkScenarioResult(
            ScenarioKey: "startup_health",
            ScenarioLabel: $"Startup (GET {config.HealthPath})",
            MetricKind: "startup",
            ValueMs: config.StartupTimeoutMs,
            P95Ms: null,
            MinMs: null,
            MaxMs: null,
            Iterations: 1,
            Success: false,
            ErrorMessage: $"Timeout after {config.StartupTimeoutMs} ms");
    }

    public static async Task<BenchmarkScenarioResult> MeasureLatencyAsync(
        BenchmarkConfig config,
        Action<string>? onStatus = null,
        CancellationToken ct = default)
    {
        var url = $"{config.BaseUrl.TrimEnd('/')}{config.HealthPath}";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // Warm-up
        onStatus?.Invoke($"Warming up ({config.WarmUpRequests} requests)...");
        for (var i = 0; i < config.WarmUpRequests; i++)
        {
            ct.ThrowIfCancellationRequested();
            try { await http.GetAsync(url, ct); }
            catch { /* ignore warm-up errors */ }
        }

        // Measured requests
        var timings = new List<double>(config.LatencyIterations);
        var errors = 0;

        for (var i = 0; i < config.LatencyIterations; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (i % 10 == 0)
                onStatus?.Invoke($"Request {i + 1}/{config.LatencyIterations}");

            var sw = Stopwatch.StartNew();
            try
            {
                var resp = await http.GetAsync(url, ct);
                sw.Stop();
                if (resp.IsSuccessStatusCode)
                {
                    timings.Add(sw.Elapsed.TotalMilliseconds);
                }
                else
                {
                    errors++;
                }
            }
            catch
            {
                sw.Stop();
                errors++;
            }
        }

        if (timings.Count == 0)
        {
            return new BenchmarkScenarioResult(
                ScenarioKey: "latency_get_health",
                ScenarioLabel: $"Latency GET {config.HealthPath}",
                MetricKind: "latency",
                ValueMs: 0,
                P95Ms: null,
                MinMs: null,
                MaxMs: null,
                Iterations: config.LatencyIterations,
                Success: false,
                ErrorMessage: $"All {config.LatencyIterations} requests failed");
        }

        timings.Sort();
        var avg = timings.Average();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[Math.Clamp(p95Index, 0, timings.Count - 1)];
        var min = timings[0];
        var max = timings[^1];

        onStatus?.Invoke($"Done: avg={avg:F1}ms p95={p95:F1}ms ({errors} errors)");

        return new BenchmarkScenarioResult(
            ScenarioKey: "latency_get_health",
            ScenarioLabel: $"Latency GET {config.HealthPath}",
            MetricKind: "latency",
            ValueMs: Math.Round(avg, 1),
            P95Ms: Math.Round(p95, 1),
            MinMs: Math.Round(min, 1),
            MaxMs: Math.Round(max, 1),
            Iterations: timings.Count,
            Success: errors < config.LatencyIterations,
            ErrorMessage: errors > 0 ? $"{errors}/{config.LatencyIterations} requests failed" : null);
    }
}
