using System.Data;
using SnowflakeExample.Services;

namespace SnowflakeExample.Apps;

/// <summary>
/// Snowflake Demo App - Interactive dashboard for exploring Snowflake databases
/// </summary>
[App(icon: Icons.Database, title: "Snowflake")]
public class SnowflakeApp : ViewBase, IHaveSecrets
{
    public override object? Build()
    {
        var snowflakeService = this.UseService<SnowflakeService>();
        var refreshToken = this.UseRefreshToken();
        
        // State management
        var databases = this.UseState<List<string>>(() => new List<string>());
        var selectedDatabase = this.UseState<string?>(() => null);
        var schemas = this.UseState<List<string>>(() => new List<string>());
        var selectedSchema = this.UseState<string?>(() => null);
        var tables = this.UseState<List<string>>(() => new List<string>());
        var selectedTable = this.UseState<string?>(() => null);
        var tableInfo = this.UseState<TableInfo?>(() => null);
        var tablePreview = this.UseState<System.Data.DataTable?>(() => null);
        var currentPage = this.UseState(1);
        const int pageSize = 30;
        
        var isLoadingStats = this.UseState(false);
        var isLoadingSchemas = this.UseState(false);
        var isLoadingTables = this.UseState(false);
        var isLoadingTableData = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        
        var totalDatabases = this.UseState(0);
        var totalSchemas = this.UseState(0);
        var totalTables = this.UseState(0);
    
        // UseEffect hooks - must be at the top
        this.UseEffect(async () =>
        {
            await LoadDatabases();
            if (databases.Value.Count > 0) await LoadStatistics(null);
        }, []);
        
        this.UseEffect(async () =>
        {
            if (string.IsNullOrEmpty(selectedDatabase.Value))
            {
                schemas.Value = new List<string>();
                ClearSelection();
                if (databases.Value.Count > 0) await LoadStatistics(null);
            }
            else
            {
                await LoadSchemas(selectedDatabase.Value);
                await LoadStatistics(selectedDatabase.Value);
            }
        }, selectedDatabase);
        
        this.UseEffect(async () =>
        {
            if (!string.IsNullOrEmpty(selectedDatabase.Value) && !string.IsNullOrEmpty(selectedSchema.Value))
            {
                await LoadTables(selectedDatabase.Value, selectedSchema.Value);
            }
            else
            {
                tables.Value = new List<string>();
                selectedTable.Value = null;
                tableInfo.Value = null;
                tablePreview.Value = null;
            }
        }, selectedSchema);
        
        this.UseEffect(async () =>
        {
            if (!string.IsNullOrEmpty(selectedDatabase.Value) 
                && !string.IsNullOrEmpty(selectedSchema.Value)
                && !string.IsNullOrEmpty(selectedTable.Value))
            {
                await LoadTablePreview(selectedDatabase.Value, selectedSchema.Value, selectedTable.Value);
            }
            else
            {
                tableInfo.Value = null;
                tablePreview.Value = null;
            }
        }, selectedTable);

        // Helper functions
        async Task<T?> TryAsync<T>(Func<Task<T>> action, string errorPrefix)
        {
            try
            {
                refreshToken.Refresh();
                return await action();
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"{errorPrefix}: {ex.Message}";
                return default;
            }
            finally
            {
                refreshToken.Refresh();
            }
        }
        
        void ClearSelection()
        {
            selectedSchema.Value = null;
            tables.Value = new List<string>();
            selectedTable.Value = null;
            tableInfo.Value = null;
            tablePreview.Value = null;
        }
        
        // Load functions
        async Task LoadDatabases()
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            var dbList = await TryAsync(() => snowflakeService.GetDatabasesAsync(), "Error loading databases");
            if (dbList != null)
            {
                databases.Value = dbList;
                totalDatabases.Value = dbList.Count;
            }
            isLoadingStats.Value = false;
        }
        
        async Task LoadStatistics(string? database = null)
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            try
            {
                refreshToken.Refresh();
                if (database == null)
                {
                    int totalSchemasCount = 0;
                    int totalTablesCount = 0;
                    foreach (var db in databases.Value)
                    {
                        try
                        {
                            var schemaList = await snowflakeService.GetSchemasAsync(db);
                            totalSchemasCount += schemaList.Count;
                            foreach (var schema in schemaList)
                            {
                                try
                                {
                                    var tableList = await snowflakeService.GetTablesAsync(db, schema);
                                    totalTablesCount += tableList.Count;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                    totalSchemas.Value = totalSchemasCount;
                    totalTables.Value = totalTablesCount;
                }
                else
                {
                    var schemaList = await snowflakeService.GetSchemasAsync(database);
                    totalSchemas.Value = schemaList.Count;
                    int totalTablesCount = 0;
                    foreach (var schema in schemaList)
                    {
                        try
                        {
                            var tableList = await snowflakeService.GetTablesAsync(database, schema);
                            totalTablesCount += tableList.Count;
                        }
                        catch { }
                    }
                    totalTables.Value = totalTablesCount;
                }
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading statistics: {ex.Message}";
            }
            finally
            {
                isLoadingStats.Value = false;
                refreshToken.Refresh();
            }
        }
        
        async Task LoadSchemas(string database)
        {
            isLoadingSchemas.Value = true;
            errorMessage.Value = null;
            ClearSelection();
            var schemaList = await TryAsync(() => snowflakeService.GetSchemasAsync(database), "Error loading schemas");
            if (schemaList != null) schemas.Value = schemaList;
            isLoadingSchemas.Value = false;
        }
        
        async Task LoadTables(string database, string schema)
        {
            isLoadingTables.Value = true;
            errorMessage.Value = null;
            selectedTable.Value = null;
            tableInfo.Value = null;
            tablePreview.Value = null;
            var tableList = await TryAsync(() => snowflakeService.GetTablesAsync(database, schema), "Error loading tables");
            if (tableList != null) tables.Value = tableList;
            isLoadingTables.Value = false;
        }
        
        async Task LoadTablePreview(string database, string schema, string table)
        {
            isLoadingTableData.Value = true;
            errorMessage.Value = null;
            var info = await TryAsync(() => snowflakeService.GetTableInfoAsync(database, schema, table), "Error loading table info");
            if (info != null) tableInfo.Value = info;
            var preview = await TryAsync(() => snowflakeService.GetTablePreviewAsync(database, schema, table), "Error loading table preview");
            if (preview != null) tablePreview.Value = preview;
            isLoadingTableData.Value = false;
        }
        
        
        // Helper variables for UI
        var hasDatabase = !string.IsNullOrEmpty(selectedDatabase.Value);
        var hasSchema = hasDatabase && !string.IsNullOrEmpty(selectedSchema.Value);
        var hasTable = hasSchema && !string.IsNullOrEmpty(selectedTable.Value);
        var isLoadingData = isLoadingStats.Value || databases.Value.Count == 0;
        
        // Statistics Cards
        var statsCards = Layout.Horizontal().Gap(4).Align(Align.TopCenter)
            | BuildStatCard("Databases", totalDatabases.Value, isLoadingStats.Value)
            | BuildStatCard(hasDatabase ? $"Schemas in {selectedDatabase.Value}" : "Schemas", totalSchemas.Value, isLoadingStats.Value)
            | BuildStatCard(hasDatabase ? $"Tables in {selectedDatabase.Value}" : "Tables", totalTables.Value, isLoadingStats.Value);
        
        // Selection Options
        var databaseOptions = databases.Value
            .Select(db => new Option<string>(db, db))
            .Prepend(new Option<string>("-- Select Database --", ""))
            .ToArray();
        
        var schemaOptions = schemas.Value
            .Select(schema => new Option<string>(schema, schema))
            .Prepend(new Option<string>("-- Select Schema --", ""))
            .ToArray();
        
        var tableOptions = tables.Value
            .Select(table => new Option<string>(table, table))
            .Prepend(new Option<string>("-- Select Table --", ""))
            .ToArray();
        
        // Left Section
        var leftSection = new Card(
            Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Database Explorer")
            | Text.Muted("Select database, schema, and table")
            | (isLoadingData
                ? BuildSkeletons(3)
                : Layout.Vertical().Gap(3)
                    | selectedDatabase.ToSelectInput(databaseOptions).Placeholder("Select a database...").WithField().Label("Database")
                    | (isLoadingSchemas.Value && hasDatabase
                        ? new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                        : selectedSchema.ToSelectInput(schemaOptions).Placeholder("Select a schema...").Disabled(!hasDatabase || schemas.Value.Count == 0).WithField().Label("Schema"))
                    | (isLoadingTables.Value && hasSchema
                        ? new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                        : selectedTable.ToSelectInput(tableOptions).Placeholder("Select a table...").Disabled(!hasSchema || tables.Value.Count == 0).WithField().Label("Table")))
            | (tableInfo.Value != null
                ? Layout.Vertical().Gap(3)
                    | Text.H4("Table Structure")
                    | Text.Muted($"{tableInfo.Value.ColumnCount} columns, {tableInfo.Value.RowCount:N0} rows")
                    | BuildColumnsTable(tableInfo.Value.Columns)
                : new Spacer())
        ).Width(Size.Fraction(0.3f));
        
        // Right Section
        var rightSection = hasTable
            ? new Card(
                Layout.Vertical().Gap(4).Padding(3)
                | Layout.Horizontal().Gap(4).Align(Align.Center)
                    | Text.H3($"{selectedDatabase.Value}.{selectedSchema.Value}.{selectedTable.Value}")
                    | new Spacer()
                    | (tableInfo.Value != null
                        ? Layout.Horizontal().Gap(3)
                            | Text.Small($"Rows: {tableInfo.Value.RowCount:N0}")
                            | Text.Small($"Columns: {tableInfo.Value.ColumnCount}")
                        : new Spacer())
                | Text.Muted("Data Preview:")
                | (isLoadingTableData.Value 
                    ? BuildSkeletons(5)
                    : (tablePreview.Value?.Rows.Count > 0
                        ? BuildDataTableWithPagination(tablePreview.Value, currentPage.Value, pageSize, currentPage)
                        : Text.Muted("No data available")))
            ).Width(Size.Fraction(0.7f))
            : new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H3("Data Preview")
                | Text.Muted("Select a table to view its data")
            ).Width(Size.Fraction(0.7f));
        
        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | Text.H2("Snowflake Database Explorer")
            | Text.Muted("Explore your Snowflake databases, schemas, and tables")
            | statsCards
            | (errorMessage.Value != null 
                ? new Card(
                    Layout.Vertical().Gap(2).Padding(2)
                        | Text.Small($"Error: {errorMessage.Value}")
                )
                : new Spacer())
            | (Layout.Horizontal().Gap(4).Align(Align.TopCenter)
                | leftSection
                | rightSection);
    }
    
    private object BuildStatCard(string label, int value, bool isLoading)
    {
        return new Card(
            Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
            | (isLoading 
                ? new Skeleton().Height(Size.Units(32)).Width(Size.Units(60))
                : Text.H2(value.ToString()))
            | Text.Muted(label)
        ).Width(Size.Fraction(0.3f));
    }
    
    private object BuildSkeletons(int count)
    {
        var layout = Layout.Vertical().Gap(2);
        foreach (var _ in Enumerable.Range(0, count))
        {
            layout = layout | new Skeleton().Height(Size.Units(24)).Width(Size.Full());
        }
        return layout;
    }
    
    private object BuildColumnsTable(List<ColumnInfo> columns)
    {
        var rows = new List<TableRow>();
        
        // Header
        rows.Add(new TableRow([
            new TableCell("Column Name").IsHeader(),
            new TableCell("Type").IsHeader(),
            new TableCell("Nullable").IsHeader()
        ]));
        
        // Data rows
        foreach (var col in columns)
        {
            rows.Add(new TableRow([
                new TableCell(col.Name),
                new TableCell(col.Type),
                new TableCell(col.Nullable ? "Yes" : "No")
            ]));
        }
        
        return new Table([.. rows]);
    }
    
    private object BuildDataTableWithPagination(System.Data.DataTable dataTable, int currentPageValue, int pageSize, IState<int> currentPageState)
    {
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return Text.Muted("No data available");
        }
        
        var columns = dataTable.Columns.Cast<DataColumn>().ToList();
        var allRows = dataTable.Rows.Cast<DataRow>().ToList();
        
        // Calculate pagination
        var totalRows = allRows.Count;
        var totalPages = (int)Math.Ceiling(totalRows / (double)pageSize);
        
        // Ensure current page is valid
        var validPage = currentPageValue < 1 ? 1 : (currentPageValue > totalPages && totalPages > 0 ? totalPages : currentPageValue);
        if (validPage != currentPageValue)
        {
            currentPageState.Value = validPage;
        }
        
        // Get rows for current page
        var startIndex = (validPage - 1) * pageSize;
        var pageRows = allRows.Skip(startIndex).Take(pageSize).ToList();
        
        // Build Table with pagination
        var tableRows = new List<TableRow>();
        
        // Header row
        var headerCells = columns.Select(col => 
            new TableCell(col.ColumnName).IsHeader()
        ).ToList();
        tableRows.Add(new TableRow([.. headerCells]));
        
        // Data rows for current page
        foreach (DataRow row in pageRows)
        {
            var dataCells = columns.Select(col =>
            {
                var value = row[col];
                var displayValue = value == DBNull.Value ? "" : value?.ToString() ?? "";
                // Truncate very long values for better display
                if (displayValue.Length > 100)
                {
                    displayValue = displayValue.Substring(0, 97) + "...";
                }
                return new TableCell(displayValue);
            }).ToList();
            tableRows.Add(new TableRow([.. dataCells]));
        }
        
        var tableView = new Table([.. tableRows]);
        
        // Build pagination
        var pagination = totalPages > 1
            ? new Pagination(
                validPage,
                totalPages,
                newPage => currentPageState.Value = newPage.Value)
            : null;
        
        return Layout.Vertical().Gap(3)
            | tableView
            | (pagination != null
                ? Layout.Horizontal().Gap(2).Align(Align.Center)
                    | pagination
                    | Text.Muted($"Showing {startIndex + 1}-{Math.Min(startIndex + pageSize, totalRows)} of {totalRows} rows")
                : Text.Small($"Showing {totalRows} row(s)"));
    }
    
    public Secret[] GetSecrets()
    {
        return
        [
            new Secret("Snowflake:Account"),
            new Secret("Snowflake:User"),
            new Secret("Snowflake:Password")
        ];
    }
}
