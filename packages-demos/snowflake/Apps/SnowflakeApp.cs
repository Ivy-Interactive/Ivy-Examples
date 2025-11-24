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
        
        var isLoadingStats = this.UseState(false);
        var isLoadingSchemas = this.UseState(false);
        var isLoadingTables = this.UseState(false);
        var isLoadingTableData = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        
        // Statistics
        var totalDatabases = this.UseState(0);
        var totalSchemas = this.UseState(0);
        var totalTables = this.UseState(0);
        
        // Load databases and statistics on mount
        async Task LoadStatistics()
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            refreshToken.Refresh();
            
            try
            {
                var dbList = await snowflakeService.GetDatabasesAsync();
                databases.Value = dbList;
                totalDatabases.Value = dbList.Count;
                
                // Calculate total schemas and tables across all databases
                int totalSchemasCount = 0;
                int totalTablesCount = 0;
                
                foreach (var db in dbList.Take(10)) // Limit to first 10 for performance
                {
                    try
                    {
                        var schemaList = await snowflakeService.GetSchemasAsync(db);
                        totalSchemasCount += schemaList.Count;
                        
                        foreach (var schema in schemaList.Take(5)) // Limit schemas per DB
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
        
        this.UseEffect(async () =>
        {
            await LoadStatistics();
        }, []);
        
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
                
                var preview = await snowflakeService.GetTablePreviewAsync(database, schema, table, 100);
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
        
        // Dashboard Statistics Cards
        var statsCards = Layout.Horizontal().Gap(4).Wrap()
            | new Card(
                Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
                | Text.H2(totalDatabases.Value.ToString())
                | Text.Muted("Databases")
                | (isLoadingStats.Value ? Text.Small("Loading...") : new Spacer())
            ).Width(Size.Fraction(0.3f))
            | new Card(
                Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
                | Text.H2(totalSchemas.Value.ToString())
                | Text.Muted("Schemas")
                | (isLoadingStats.Value ? Text.Small("Loading...") : new Spacer())
            ).Width(Size.Fraction(0.3f))
            | new Card(
                Layout.Vertical().Gap(2).Align(Align.Center).Padding(3)
                | Text.H2(totalTables.Value.ToString())
                | Text.Muted("Tables")
                | (isLoadingStats.Value ? Text.Small("Loading...") : new Spacer())
            ).Width(Size.Fraction(0.3f));
        
        // Database Selection
        var databaseSection = new Card(
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H3("Databases")
            | Text.Muted("Select a database to explore")
            | (databases.Value.Count > 0
                ? Layout.Vertical().Gap(2)
                    | databases.Value.Select(db => 
                        (selectedDatabase.Value == db
                            ? new Button(db, onClick: async _ =>
                            {
                                selectedDatabase.Value = db;
                                await LoadSchemas(db);
                            })
                                .Primary()
                                .Icon(Icons.Database)
                            : new Button(db, onClick: async _ =>
                            {
                                selectedDatabase.Value = db;
                                await LoadSchemas(db);
                            })
                                .Secondary()
                                .Icon(Icons.Database))
                    ).ToArray()
                : (isLoadingStats.Value 
                    ? Text.Small("Loading databases...") 
                    : Text.Small("No databases found")))
        ).Width(Size.Fraction(0.25f));
        
        // Schema Selection
        var schemaSection = selectedDatabase.Value != null
            ? new Card(
                Layout.Vertical().Gap(3).Padding(2)
                | Text.H3("Schemas")
                | Text.Muted($"In {selectedDatabase.Value}")
                | (schemas.Value.Count > 0
                    ? Layout.Vertical().Gap(2)
                        | schemas.Value.Select(schema => 
                            (selectedSchema.Value == schema
                                ? new Button(schema, onClick: async _ =>
                                {
                                    selectedSchema.Value = schema;
                                    await LoadTables(selectedDatabase.Value!, schema);
                                })
                                    .Primary()
                                    .Icon(Icons.Folder)
                                : new Button(schema, onClick: async _ =>
                                {
                                    selectedSchema.Value = schema;
                                    await LoadTables(selectedDatabase.Value!, schema);
                                })
                                    .Secondary()
                                    .Icon(Icons.Folder))
                        ).ToArray()
                    : (isLoadingSchemas.Value 
                        ? Text.Small("Loading schemas...") 
                        : Text.Small("No schemas found")))
            ).Width(Size.Fraction(0.25f))
            : new Card(
                Layout.Vertical().Gap(3).Padding(2)
                | Text.H3("Schemas")
                | Text.Muted("Select a database first")
            ).Width(Size.Fraction(0.25f));
        
        // Table Selection
        var tableSection = selectedSchema.Value != null && selectedDatabase.Value != null
            ? new Card(
                Layout.Vertical().Gap(3).Padding(2)
                | Text.H3("Tables")
                | Text.Muted($"In {selectedDatabase.Value}.{selectedSchema.Value}")
                | (tables.Value.Count > 0
                    ? Layout.Vertical().Gap(2)
                        | tables.Value.Select(table => 
                            (selectedTable.Value == table
                                ? new Button(table, onClick: async _ =>
                                {
                                    selectedTable.Value = table;
                                    await LoadTablePreview(selectedDatabase.Value!, selectedSchema.Value!, table);
                                })
                                    .Primary()
                                    .Icon(Icons.Table)
                                : new Button(table, onClick: async _ =>
                                {
                                    selectedTable.Value = table;
                                    await LoadTablePreview(selectedDatabase.Value!, selectedSchema.Value!, table);
                                })
                                    .Secondary()
                                    .Icon(Icons.Table))
                        ).ToArray()
                    : (isLoadingTables.Value 
                        ? Text.Small("Loading tables...") 
                        : Text.Small("No tables found")))
            ).Width(Size.Fraction(0.25f))
            : new Card(
                Layout.Vertical().Gap(3).Padding(2)
                | Text.H3("Tables")
                | Text.Muted("Select a schema first")
            ).Width(Size.Fraction(0.25f));
        
        // Table Preview Section
        var previewSection = tableInfo.Value != null && tablePreview.Value != null
            ? new Card(
                Layout.Vertical().Gap(4).Padding(2)
                | Layout.Horizontal().Gap(4).Align(Align.Center)
                    | Text.H3($"{selectedDatabase.Value}.{selectedSchema.Value}.{selectedTable.Value}")
                    | new Spacer()
                    | Layout.Horizontal().Gap(3)
                        | Text.Small($"Rows: {tableInfo.Value.RowCount:N0}")
                        | Text.Small($"Columns: {tableInfo.Value.ColumnCount}")
                | Text.Muted("Table Structure:")
                | BuildColumnsTable(tableInfo.Value.Columns)
                | Text.Muted("Preview (first 100 rows):")
                | (isLoadingTableData.Value 
                    ? Text.Small("Loading data...") 
                    : BuildDataTable(tablePreview.Value))
            ).Width(Size.Fraction(0.7f))
            : new Card(
                Layout.Vertical().Gap(3).Padding(2)
                | Text.H3("Table Preview")
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
            | Layout.Horizontal().Gap(4).Align(Align.TopCenter)
                | databaseSection
                | schemaSection
                | tableSection
            | Layout.Horizontal().Gap(4).Align(Align.TopCenter)
                | previewSection;
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
    
    private object BuildDataTable(System.Data.DataTable table)
    {
        var rows = table.AsEnumerable().ToList();
        var tableColumns = table.Columns.Cast<DataColumn>().ToList();
        var listOfTableRows = new List<TableRow>();
        
        // Header row
        var headerCells = tableColumns.Select(col => 
            new TableCell(col.ColumnName).IsHeader()
        ).ToList();
        listOfTableRows.Add(new TableRow([.. headerCells]));
        
        // Data rows - limit to first 100 rows for performance
        var displayRows = rows.Take(100).ToList();
        foreach (var row in displayRows)
        {
            var dataCells = row.ItemArray.Select(value => 
                new TableCell(value?.ToString() ?? "")
            ).ToList();
            listOfTableRows.Add(new TableRow([.. dataCells]));
        }
        
        var tableView = new Table([.. listOfTableRows]);
        
        // Show message if there are more rows
        if (rows.Count > 100)
        {
            return Layout.Vertical().Gap(2)
                | tableView
                | Text.Small($"Showing first 100 of {rows.Count} rows");
        }
        
        return tableView;
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
