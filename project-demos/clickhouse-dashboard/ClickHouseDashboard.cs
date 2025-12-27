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

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

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

public class TableStats
{
    public string TableName { get; set; } = "";
    public long RowCount { get; set; }
    public double SizeMB { get; set; }
}

[App(icon: Icons.ChartBar, title: "ClickHouse Dashboard")]
public class DashboardApp : ViewBase
{
    private static async Task<List<TableStats>> LoadFromClickHouse()
    {
        try
        {
            var connectionString = "Host=localhost;Port=8123;Username=default;Password=default;Database=default;Protocol=http";
            
            await using var connection = new ClickHouseConnection(connectionString);
            await connection.OpenAsync();

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

    public override object? Build()
    {
        var tableData = this.UseState<List<TableStats>>(() => new List<TableStats>());
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

        var headerRow = Layout.Grid().Columns(3).Gap(2).Padding(2)
            | Text.Small("Table").Bold()
            | Text.Small("Rows").Bold()
            | Text.Small("Size (MB)").Bold();
        
        var dataRows = Layout.Vertical().Gap(1);
        foreach (var t in tableData.Value)
        {
            dataRows = dataRows | (Layout.Grid().Columns(3).Gap(2).Padding(2)
                | Text.Small(t.TableName)
                | Text.Small(t.RowCount.ToString("N0"))
                | Text.Small(t.SizeMB.ToString("N2")));
        }
        
        var tablesTable = Layout.Vertical().Gap(2)
            | headerRow
            | new Separator()
            | Layout.Vertical().Gap(1)
                | dataRows;

        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | Text.H1("ClickHouse Dashboard")
            | Text.Label($"{totalTables} Tables").Bold().Muted()
            | metrics.Width(Size.Fraction(0.9f))
            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.9f))
                | new Card(pieChart).Title("Size Distribution")
                | new Card(barChart).Title("Top Tables by Rows"))
            | new Card(tablesTable).Title("All Tables").Width(Size.Fraction(0.9f));
    }
}
