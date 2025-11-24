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
        var pageSize = 30;
        
        var isLoadingStats = this.UseState(false);
        var isLoadingSchemas = this.UseState(false);
        var isLoadingTables = this.UseState(false);
        var isLoadingTableData = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        
        // Statistics
        var totalDatabases = this.UseState(0);
        var totalSchemas = this.UseState(0);
        var totalTables = this.UseState(0);
        
        // Load databases on mount
        async Task LoadDatabases()
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            refreshToken.Refresh();
            
            try
            {
                var dbList = await snowflakeService.GetDatabasesAsync();
                databases.Value = dbList;
                totalDatabases.Value = dbList.Count;
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading databases: {ex.Message}";
            }
            finally
            {
                isLoadingStats.Value = false;
                refreshToken.Refresh();
            }
        }
        
        // Load statistics - for all databases or selected database
        async Task LoadStatistics(string? database = null)
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            refreshToken.Refresh();
            
            try
            {
                if (database == null)
                {
                    // Calculate statistics for all databases
                    int totalSchemasCount = 0;
                    int totalTablesCount = 0;
                    
                    // Count all schemas and tables across all databases
                    foreach (var db in databases.Value)
                    {
                        try
                        {
                            var schemaList = await snowflakeService.GetSchemasAsync(db);
                            totalSchemasCount += schemaList.Count;
                            
                            // Count tables in all schemas
                            foreach (var schema in schemaList)
                            {
                                try
                                {
                                    var tableList = await snowflakeService.GetTablesAsync(db, schema);
                                    totalTablesCount += tableList.Count;
                                }
                                catch
                                {
                                    // Skip if can't access tables
                                }
                            }
                        }
                        catch
                        {
                            // Skip if can't access schemas
                        }
                    }
                    
                    totalSchemas.Value = totalSchemasCount;
                    totalTables.Value = totalTablesCount;
                }
                else
                {
                    // Calculate statistics for selected database only
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
                        catch
                        {
                            // Skip if can't access tables
                        }
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
        
        // Load databases on mount
        this.UseEffect(async () =>
        {
            await LoadDatabases();
            // Load statistics after databases are loaded
            if (databases.Value.Count > 0)
            {
                await LoadStatistics(null);
            }
        }, []);
        
        // Load statistics when database changes
        this.UseEffect(async () =>
        {
            if (databases.Value.Count > 0)
            {
                await LoadStatistics(selectedDatabase.Value);
            }
        }, selectedDatabase);
        
        // Load schemas when database is selected
        async Task LoadSchemas(string database)
        {
            isLoadingSchemas.Value = true;
            errorMessage.Value = null;
            selectedSchema.Value = null;
            tables.Value = new List<string>();
            selectedTable.Value = null;
            tableInfo.Value = null;
            tablePreview.Value = null;
            refreshToken.Refresh();
            
            try
            {
                var schemaList = await snowflakeService.GetSchemasAsync(database);
                schemas.Value = schemaList;
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading schemas: {ex.Message}";
            }
            finally
            {
                isLoadingSchemas.Value = false;
                refreshToken.Refresh();
            }
        }
        
        // Load tables when schema is selected
        async Task LoadTables(string database, string schema)
        {
            isLoadingTables.Value = true;
            errorMessage.Value = null;
            selectedTable.Value = null;
            tableInfo.Value = null;
            tablePreview.Value = null;
            refreshToken.Refresh();
            
            try
            {
                var tableList = await snowflakeService.GetTablesAsync(database, schema);
                tables.Value = tableList;
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading tables: {ex.Message}";
            }
            finally
            {
                isLoadingTables.Value = false;
                refreshToken.Refresh();
            }
        }
        
        // Load table preview when table is selected
        async Task LoadTablePreview(string database, string schema, string table)
        {
            isLoadingTableData.Value = true;
            errorMessage.Value = null;
            refreshToken.Refresh();
            
            try
            {
                var info = await snowflakeService.GetTableInfoAsync(database, schema, table);
                tableInfo.Value = info;
                
                var preview = await snowflakeService.GetTablePreviewAsync(database, schema, table);
                tablePreview.Value = preview;
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading table data: {ex.Message}";
            }
            finally
            {
                isLoadingTableData.Value = false;
                refreshToken.Refresh();
            }
        }
        
        // Handle database selection
        this.UseEffect(async () =>
        {
            if (selectedDatabase.Value != null && !string.IsNullOrEmpty(selectedDatabase.Value))
            {
                await LoadSchemas(selectedDatabase.Value);
                await LoadStatistics(selectedDatabase.Value);
            }
            else
            {
                schemas.Value = new List<string>();
                selectedSchema.Value = null;
                tables.Value = new List<string>();
                selectedTable.Value = null;
                tableInfo.Value = null;
                tablePreview.Value = null;
                await LoadStatistics(null);
            }
        }, selectedDatabase);
        
        // Handle schema selection
        this.UseEffect(async () =>
        {
            if (selectedDatabase.Value != null && !string.IsNullOrEmpty(selectedDatabase.Value) 
                && selectedSchema.Value != null && !string.IsNullOrEmpty(selectedSchema.Value))
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
        
        // Handle table selection
        this.UseEffect(async () =>
        {
            if (selectedDatabase.Value != null && !string.IsNullOrEmpty(selectedDatabase.Value)
                && selectedSchema.Value != null && !string.IsNullOrEmpty(selectedSchema.Value)
                && selectedTable.Value != null && !string.IsNullOrEmpty(selectedTable.Value))
            {
                await LoadTablePreview(selectedDatabase.Value, selectedSchema.Value, selectedTable.Value);
            }
            else
            {
                tableInfo.Value = null;
                tablePreview.Value = null;
            }
        }, selectedTable);
        
        // Dashboard Statistics Cards
        var statsCards = Layout.Horizontal().Gap(4).Align(Align.TopCenter)
            | new Card(
                Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
                | (isLoadingStats.Value 
                    ? new Skeleton().Height(Size.Units(32)).Width(Size.Units(60))
                    : Text.H2(totalDatabases.Value.ToString()))
                | Text.Muted("Databases")
            ).Width(Size.Fraction(0.3f))
            | new Card(
                Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
                | (isLoadingStats.Value 
                    ? new Skeleton().Height(Size.Units(32)).Width(Size.Units(60))
                    : Text.H2(totalSchemas.Value.ToString()))
                | Text.Muted(selectedDatabase.Value != null ? $"Schemas in {selectedDatabase.Value}" : "Schemas")
            ).Width(Size.Fraction(0.3f))
            | new Card(
                Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
                | (isLoadingStats.Value 
                    ? new Skeleton().Height(Size.Units(32)).Width(Size.Units(60))
                    : Text.H2(totalTables.Value.ToString()))
                | Text.Muted(selectedDatabase.Value != null ? $"Tables in {selectedDatabase.Value}" : "Tables")
            ).Width(Size.Fraction(0.3f));
        
        // Database Selection Options
        var databaseOptions = databases.Value
            .Select(db => new Option<string>(db, db))
            .Prepend(new Option<string>("-- Select Database --", ""))
            .ToArray();
        
        // Schema Selection Options
        var schemaOptions = schemas.Value
            .Select(schema => new Option<string>(schema, schema))
            .Prepend(new Option<string>("-- Select Schema --", ""))
            .ToArray();
        
        // Table Selection Options
        var tableOptions = tables.Value
            .Select(table => new Option<string>(table, table))
            .Prepend(new Option<string>("-- Select Table --", ""))
            .ToArray();
        
        // Left Section - Database Selection and Column Information
        var leftSection = new Card(
            Layout.Vertical().Gap(4).Padding(3)
            | Text.H3("Database Explorer")
            | Text.Muted("Select database, schema, and table")
            | (isLoadingStats.Value || databases.Value.Count == 0
                ? Layout.Vertical().Gap(2)
                    | new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                    | new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                    | new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                : Layout.Vertical().Gap(3)
                    | selectedDatabase
                        .ToSelectInput(databaseOptions)
                        .Placeholder("Select a database...")
                        .WithField()
                        .Label("Database")
                    | (isLoadingSchemas.Value && selectedDatabase.Value != null && !string.IsNullOrEmpty(selectedDatabase.Value)
                        ? new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                        : selectedSchema
                            .ToSelectInput(schemaOptions)
                            .Placeholder("Select a schema...")
                            .Disabled(selectedDatabase.Value == null || string.IsNullOrEmpty(selectedDatabase.Value) || schemas.Value.Count == 0)
                            .WithField()
                            .Label("Schema"))
                    | (isLoadingTables.Value && selectedSchema.Value != null && !string.IsNullOrEmpty(selectedSchema.Value) && selectedDatabase.Value != null
                        ? new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                        : selectedTable
                            .ToSelectInput(tableOptions)
                            .Placeholder("Select a table...")
                            .Disabled(selectedSchema.Value == null || string.IsNullOrEmpty(selectedSchema.Value) || selectedDatabase.Value == null || tables.Value.Count == 0)
                            .WithField()
                            .Label("Table")))
            | (tableInfo.Value != null
                ? Layout.Vertical().Gap(3)
                    | Text.H4("Table Structure")
                    | Text.Muted($"{tableInfo.Value.ColumnCount} columns, {tableInfo.Value.RowCount:N0} rows")
                    | BuildColumnsTable(tableInfo.Value.Columns)
                : new Spacer())
        ).Width(Size.Fraction(0.3f));
        
        // Right Section - Data Preview
        var rightSection = selectedTable.Value != null && selectedDatabase.Value != null && selectedSchema.Value != null
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
                    ? Layout.Vertical().Gap(2)
                        | new Skeleton().Height(Size.Units(24)).Width(Size.Full())
                        | new Skeleton().Height(Size.Units(16)).Width(Size.Full())
                        | new Skeleton().Height(Size.Units(16)).Width(Size.Full())
                        | new Skeleton().Height(Size.Units(16)).Width(Size.Full())
                        | new Skeleton().Height(Size.Units(16)).Width(Size.Full())
                    : (tablePreview.Value != null && tablePreview.Value.Rows.Count > 0
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
