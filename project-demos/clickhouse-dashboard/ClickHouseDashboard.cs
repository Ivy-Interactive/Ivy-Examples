#:package Ivy@1.2.6
#:package ClickHouse.Driver@0.9.0

global using Ivy;
global using Ivy.Apps;
global using Ivy.Chrome;
global using Ivy.Core;
global using Ivy.Core.Hooks;
global using Ivy.Hooks;
global using Ivy.Shared;
global using Ivy.Views;
global using Ivy.Views.Charts;
global using Ivy.Views.DataTables;
global using ClickHouse.Driver.ADO;
global using System.Data;
global using System.Globalization;
global using System.Diagnostics;
global using System.Net.Sockets;
global using System.Threading;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

// Check if ClickHouse is running, if not try to start it via Docker
await EnsureClickHouseRunning();

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings()
    .DefaultApp<DashboardApp>()
    .UseTabs(preventDuplicates: true);

server.UseChrome(chromeSettings);
await server.RunAsync();

static async Task EnsureClickHouseRunning()
{
    if (await IsPortOpen("localhost", 8123))
        return;

    Console.WriteLine("Starting ClickHouse via Docker...");
    
    try
    {
        var dockerComposePath = Path.Combine(Directory.GetCurrentDirectory(), "docker-compose.yml");
        if (!File.Exists(dockerComposePath))
            dockerComposePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "docker-compose.yml");

        if (File.Exists(dockerComposePath))
        {
            var dir = Path.GetDirectoryName(dockerComposePath);
            Process.Start(new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = "up -d",
                WorkingDirectory = dir,
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();

            // Wait for ClickHouse to start (max 30 seconds)
            for (int i = 0; i < 30; i++)
            {
                if (await IsPortOpen("localhost", 8123)) break;
                await Task.Delay(1000);
            }
        }
    }
    catch
    {
        Console.WriteLine("Could not start ClickHouse. Please run: docker-compose up -d");
    }
}

static async Task<bool> IsPortOpen(string host, int port)
{
    try
    {
        using var client = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await client.ConnectAsync(host, port, cts.Token);
        return true;
    }
    catch
    {
        return false;
    }
}

public class TableStats
{
    public string TableName { get; set; } = "";
    public long RowCount { get; set; }
    public double SizeMB { get; set; }
}

public class TransactionStatusStats
{
    public string Status { get; set; } = "";
    public long Count { get; set; }
    public double TotalAmount { get; set; }
}

public class LogTimeline
{
    public string Date { get; set; } = "";
    public long Count { get; set; }
}

public class UserStatusStats
{
    public string Status { get; set; } = "";
    public long Count { get; set; }
}

[App(icon: Icons.ChartBar, title: "ClickHouse Dashboard")]
public class DashboardApp : ViewBase
{
    private static async Task<ClickHouseConnection> GetConnection()
    {
        var connectionString = "Host=localhost;Port=8123;Username=default;Password=default;Database=default;Protocol=http";
        
        // Retry connection up to 5 times with delay
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                var connection = new ClickHouseConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                if (attempt == 5)
                    throw new Exception($"Failed to connect to ClickHouse after {attempt} attempts: {ex.Message}", ex);
                
                await Task.Delay(1000 * attempt); // Exponential backoff: 1s, 2s, 3s, 4s
            }
        }
        
        throw new Exception("Failed to establish connection to ClickHouse");
    }

    private static async Task<List<TableStats>> LoadFromClickHouse()
    {
        try
        {
            await using var connection = await GetConnection();

            var sql = @"SELECT 
                name as TableName,
                total_rows as RowCount,
                total_bytes / (1024 * 1024) as SizeMB
            FROM system.tables
            WHERE database = currentDatabase()
            AND engine NOT LIKE '%View%'
            ORDER BY total_rows DESC";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            var tables = new List<TableStats>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var rowCountValue = reader.GetValue(1);
                var sizeMBValue = reader.GetValue(2);
                
                tables.Add(new TableStats
                {
                    TableName = reader.GetString(0),
                    RowCount = Convert.ToInt64(rowCountValue),
                    SizeMB = Convert.ToDouble(sizeMBValue)
                });
            }

            return tables;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load data from ClickHouse: {ex.Message}", ex);
        }
    }

    private static async Task<List<TransactionStatusStats>> LoadTransactionStatuses()
    {
        try
        {
            await using var connection = await GetConnection();
            var sql = @"SELECT status as Status, count() as Count, sum(amount) as TotalAmount 
                       FROM transactions 
                       GROUP BY status 
                       ORDER BY Count DESC";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var results = new List<TransactionStatusStats>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new TransactionStatusStats
                {
                    Status = reader.GetString(0),
                    Count = Convert.ToInt64(reader.GetValue(1)),
                    TotalAmount = Convert.ToDouble(reader.GetValue(2))
                });
            }
            return results;
        }
        catch { return new List<TransactionStatusStats>(); }
    }

    private static async Task<List<LogTimeline>> LoadLogTimeline()
    {
        try
        {
            await using var connection = await GetConnection();
            var sql = @"SELECT toDate(timestamp) as Date, count() as Count 
                       FROM logs 
                       WHERE timestamp >= now() - INTERVAL 30 DAY
                       GROUP BY Date 
                       ORDER BY Date";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var results = new List<LogTimeline>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var date = reader.GetValue(0);
                results.Add(new LogTimeline
                {
                    Date = date is DateTime dt ? dt.ToString("MMM dd") : date.ToString() ?? "",
                    Count = Convert.ToInt64(reader.GetValue(1))
                });
            }
            return results;
        }
        catch { return new List<LogTimeline>(); }
    }

    private static async Task<List<UserStatusStats>> LoadUserStatuses()
    {
        try
        {
            await using var connection = await GetConnection();
            var sql = @"SELECT status as Status, count() as Count 
                       FROM users 
                       GROUP BY status 
                       ORDER BY Count DESC";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var results = new List<UserStatusStats>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new UserStatusStats
                {
                    Status = reader.GetString(0),
                    Count = Convert.ToInt64(reader.GetValue(1))
                });
            }
            return results;
        }
        catch { return new List<UserStatusStats>(); }
    }

    public override object? Build()
    {
        var tableData = this.UseState<List<TableStats>>(() => new List<TableStats>());
        var transactionStatuses = this.UseState<List<TransactionStatusStats>>(() => new List<TransactionStatusStats>());
        var logTimeline = this.UseState<List<LogTimeline>>(() => new List<LogTimeline>());
        var userStatuses = this.UseState<List<UserStatusStats>>(() => new List<UserStatusStats>());
        
        var refreshToken = this.UseRefreshToken();
        var isLoading = this.UseState(true);
        var errorMessage = this.UseState<string?>();

        this.UseEffect(async () =>
        {
            isLoading.Value = true;
            errorMessage.Value = null;
            try
            {
                var data = await LoadFromClickHouse();
                tableData.Value = data;
                
                // Load analytics data in parallel
                var tasks = new List<Task>
                {
                    Task.Run(async () => transactionStatuses.Value = await LoadTransactionStatuses()),
                    Task.Run(async () => logTimeline.Value = await LoadLogTimeline()),
                    Task.Run(async () => userStatuses.Value = await LoadUserStatuses())
                };
                
                await Task.WhenAll(tasks);
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                errorMessage.Value = ex.Message;
            }
            finally
            {
                isLoading.Value = false;
            }
        }, [EffectTrigger.AfterInit()]);

        if (isLoading.Value)
        {
            return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
                | Text.H1("ClickHouse Dashboard")
                | Text.Label("Loading data from ClickHouse...").Bold().Muted()
                | Layout.Center() | new Skeleton().Height(Size.Units(200)).Width(Size.Fraction(0.9f));
        }

        if (errorMessage.Value != null)
        {
            return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
                | Text.H1("ClickHouse Dashboard")
                | Layout.Center() | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("Connection Error")
                        | Text.Small(errorMessage.Value).Color(Colors.Red)
                        | Text.Small("Make sure ClickHouse is running on localhost:8123").Muted()
                ).Width(Size.Fraction(0.6f));
        }

        if (tableData.Value.Count == 0)
        {
            return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
                | Text.H1("ClickHouse Dashboard")
                | Layout.Center() | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("No Data")
                        | Text.Small("No tables found in the database").Muted()
                ).Width(Size.Fraction(0.6f));
        }

        var totalRows = tableData.Value.Sum(t => t.RowCount);
        var totalTables = tableData.Value.Count;
        var totalSizeMB = tableData.Value.Sum(t => t.SizeMB);
        var avgRows = tableData.Value.Average(t => (double)t.RowCount);

        var metrics = Layout.Grid().Columns(4).Gap(3)
            | new Card(Text.H3(totalRows.ToString("N0"))).Title("Total Rows").Icon(Icons.Database)
            | new Card(Text.H3(totalTables.ToString())).Title("Tables").Icon(Icons.Table)
            | new Card(Text.H3(totalSizeMB.ToString("N1") + " MB")).Title("Total Size").Icon(Icons.ArchiveX)
            | new Card(Text.H3(avgRows.ToString("N0"))).Title("Avg Rows/Table").Icon(Icons.ChartBar);

        // Database Overview Charts
        var pieChart = tableData.Value.ToPieChart(
            dimension: t => t.TableName,
            measure: t => t.Sum(f => f.SizeMB),
            PieChartStyles.Dashboard,
            new PieChartTotal(totalSizeMB.ToString("N1"), "MB"));

        var topTables = tableData.Value
            .OrderByDescending(t => t.RowCount)
            .Take(6)
            .Select(t => new { Table = t.TableName, Rows = (double)t.RowCount })
            .ToList();

        var barChart = topTables.ToBarChart()
            .Dimension("Table", e => e.Table)
            .Measure("Rows", e => e.Sum(f => f.Rows));

        // Transaction Statuses
        var transactionStatusChart = transactionStatuses.Value.Count > 0
            ? transactionStatuses.Value.ToPieChart(
                dimension: t => t.Status,
                measure: t => t.Sum(f => (double)f.Count),
                PieChartStyles.Dashboard,
                new PieChartTotal(transactionStatuses.Value.Sum(t => t.Count).ToString("N0"), "Transactions"))
            : null;

        // Logs Timeline
        var logTimelineChart = logTimeline.Value.Count > 0
            ? logTimeline.Value.ToLineChart(
                e => e.Date,
                [e => e.Sum(f => (double)f.Count)],
                LineChartStyles.Dashboard)
            : null;

        // User Statuses
        var userStatusChart = userStatuses.Value.Count > 0
            ? userStatuses.Value.ToPieChart(
                dimension: u => u.Status,
                measure: u => u.Sum(f => (double)f.Count),
                PieChartStyles.Dashboard,
                new PieChartTotal(userStatuses.Value.Sum(u => u.Count).ToString("N0"), "Users"))
            : null;

#pragma warning disable IL2026, IL3050
        var tablesDataTable = tableData.Value.AsQueryable()
            .ToDataTable()
            .Header(t => t.TableName, "Table")
            .Header(t => t.RowCount, "Rows")
            .Header(t => t.SizeMB, "Size (MB)")
            .Height(Size.Units(90));
#pragma warning restore IL2026, IL3050

        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | Text.H1("ClickHouse Dashboard")
            | Text.Label("Analytics and monitoring dashboard for ClickHouse database").Bold().Muted()
            | metrics.Width(Size.Fraction(0.9f))
            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.9f))
                | new Card(pieChart).Title("Size Distribution")
                | new Card(barChart).Title("Top Tables by Rows"))
            | (Layout.Grid().Columns(3).Gap(3).Width(Size.Fraction(0.9f))
                | new Card(transactionStatusChart).Title("Transaction Statuses")
                | new Card(logTimelineChart).Title("Logs Timeline (30 days)")
                | new Card(userStatusChart).Title("User Statuses"))
            | new Card(tablesDataTable).Title("All Tables").Width(Size.Fraction(0.9f));
    }
}
