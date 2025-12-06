using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using SnowflakeExample.Services;

namespace SnowflakeExample.Apps;

/// <summary>
/// Snowflake Demo App - Interactive dashboard for exploring Snowflake databases
/// </summary>
[App(icon: Icons.Database, title: "Snowflake")]
public class SnowflakeApp : ViewBase
{
    public override object? Build()
    {
        var refreshToken = this.UseRefreshToken();
        
        // Use state to track verification status - this allows the UI to reactively update
        var isVerified = this.UseState(() => VerifiedCredentials.IsVerified);
        
        // Subscribe to verification status changes and update state + trigger refresh
        this.UseEffect(() =>
        {
            void OnVerificationStatusChanged()
            {
                isVerified.Value = VerifiedCredentials.IsVerified;
                refreshToken.Refresh();
            }
            
            VerifiedCredentials.VerificationStatusChanged += OnVerificationStatusChanged;
            
            // Also sync on mount
            isVerified.Value = VerifiedCredentials.IsVerified;
        }, [EffectTrigger.AfterInit()]);
        
        // Check if credentials are verified - show instruction card if not verified
        if (!isVerified.Value)
        {
            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(4).Padding(4)
                    | Text.H3("Authentication Required")
                    | Text.Muted("Please enter your Snowflake credentials to access this application.")
                    | Text.Markdown("**To get started:**")
                    | (Layout.Vertical().Gap(3).Padding(4)
                        | Text.Markdown("**1.** Navigate to **Snowflake Settings** app")
                        | Text.Markdown("**2.** Click **Enter Credentials** button")
                        | Text.Markdown("**3.** Enter your Snowflake account credentials")
                        | Text.Markdown("**4.** After successful verification, the dashboard will appear automatically"))

                    | Text.Small("Your credentials are securely stored and verified before accessing Snowflake databases.")
                ).Width(Size.Fraction(0.5f));
        }
        var snowflakeService = this.UseService<SnowflakeService>();
        
        // State management
        var databases = this.UseState<List<string>>(() => new List<string>());
        var selectedDatabase = this.UseState<string?>(() => null);
        var schemas = this.UseState<List<string>>(() => new List<string>());
        var selectedSchema = this.UseState<string?>(() => null);
        var tables = this.UseState<List<string>>(() => new List<string>());
        var selectedTable = this.UseState<string?>(() => null);
        var tableInfo = this.UseState<TableInfo?>(() => null);
        var tablePreview = this.UseState<System.Data.DataTable?>(() => null);
        var activeTab = this.UseState(0); // 0 = Data, 1 = Structure
        
        var isLoadingStats = this.UseState(false);
        var isLoadingSchemas = this.UseState(false);
        var isLoadingTables = this.UseState(false);
        var isLoadingTableData = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        
        var totalDatabases = this.UseState(0);
        var totalSchemas = this.UseState(0);
        var totalTables = this.UseState(0);
        var totalSchemasAll = this.UseState(0);
        var totalTablesAll = this.UseState(0);
        var totalTablesInSchema = this.UseState(0); // Tables in selected schema
    
        // UseEffect hooks - must be at the top
        this.UseEffect(async () =>
        {
            // Only load data if credentials are verified
            if (!isVerified.Value)
            {
                return;
            }
            
            await LoadDatabases();
            if (databases.Value.Count > 0) await LoadStatistics(null);
        }, []);
        
        this.UseEffect(async () =>
        {
            if (!isVerified.Value) return;
            
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
            if (!isVerified.Value) return;
            
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
                totalTablesInSchema.Value = 0;
            }
        }, selectedSchema);
        
        this.UseEffect(async () =>
        {
            if (!isVerified.Value) return;
            
            if (!string.IsNullOrEmpty(selectedDatabase.Value) 
                && !string.IsNullOrEmpty(selectedSchema.Value)
                && !string.IsNullOrEmpty(selectedTable.Value))
            {
                activeTab.Value = 0; // Reset to Data tab when table changes
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
            errorMessage.Value = null;
            var dbList = await TryAsync(() => snowflakeService.GetDatabasesAsync(), "Error loading databases");
            if (dbList != null)
            {
                databases.Value = dbList;
                // Don't update totalDatabases here - it will be updated in LoadStatistics along with other metrics
            }
        }
        
        async Task LoadStatistics(string? database = null)
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            try
            {
                refreshToken.Refresh();
                int databasesCount = 0;
                int schemasCount = 0;
                int tablesCount = 0;
                int schemasAllCount = 0;
                int tablesAllCount = 0;
                
                if (database == null)
                {
                    databasesCount = databases.Value.Count;
                    
                    foreach (var db in databases.Value)
                    {
                        try
                        {
                            var schemaList = await snowflakeService.GetSchemasAsync(db);
                            schemasAllCount += schemaList.Count;
                            foreach (var schema in schemaList)
                            {
                                try
                                {
                                    var tableList = await snowflakeService.GetTablesAsync(db, schema);
                                    tablesAllCount += tableList.Count;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                    schemasCount = schemasAllCount;
                    tablesCount = tablesAllCount;
                }
                else
                {
                    databasesCount = databases.Value.Count;
                    var schemaList = await snowflakeService.GetSchemasAsync(database);
                    schemasCount = schemaList.Count;
                    foreach (var schema in schemaList)
                    {
                        try
                        {
                            var tableList = await snowflakeService.GetTablesAsync(database, schema);
                            tablesCount += tableList.Count;
                        }
                        catch { }
                    }
                    // Keep all values for progress calculation
                    schemasAllCount = totalSchemasAll.Value;
                    tablesAllCount = totalTablesAll.Value;
                }
                
                // Update all values simultaneously after a small delay to ensure UI updates together
                await Task.Yield();
                totalDatabases.Value = databasesCount;
                totalSchemas.Value = schemasCount;
                totalTables.Value = tablesCount;
                if (database == null)
                {
                    totalSchemasAll.Value = schemasAllCount;
                    totalTablesAll.Value = tablesAllCount;
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
            if (tableList != null)
            {
                tables.Value = tableList;
                totalTablesInSchema.Value = tableList.Count;
            }
            else
            {
                totalTablesInSchema.Value = 0;
            }
            isLoadingTables.Value = false;
        }
        
        async Task LoadTablePreview(string database, string schema, string table)
        {
            isLoadingTableData.Value = true;
            errorMessage.Value = null;
            var info = await TryAsync(() => snowflakeService.GetTableInfoAsync(database, schema, table), "Error loading table info");
            if (info != null) tableInfo.Value = info;
            var preview = await TryAsync(() => snowflakeService.GetTablePreviewAsync(database, schema, table, 1000), "Error loading table preview");
            if (preview != null) tablePreview.Value = preview;
            isLoadingTableData.Value = false;
        }
        
        
        // Helper variables for UI
        var hasDatabase = !string.IsNullOrEmpty(selectedDatabase.Value);
        var hasSchema = hasDatabase && !string.IsNullOrEmpty(selectedSchema.Value);
        var hasTable = hasSchema && !string.IsNullOrEmpty(selectedTable.Value);
        var isLoadingData = isLoadingStats.Value || databases.Value.Count == 0;
        
        // Statistics Cards - show total counts when no database selected, specific counts when database is selected
        var currentSelectedDb = selectedDatabase.Value;
        var currentSelectedSchema = selectedSchema.Value;
        var currentHasDatabase = !string.IsNullOrEmpty(currentSelectedDb);
        var currentHasSchema = currentHasDatabase && !string.IsNullOrEmpty(currentSelectedSchema);
        
        // Calculate values for cards
        var databasesDisplayValue = currentHasDatabase ? 1 : totalDatabases.Value;
        var schemasDisplayValue = totalSchemas.Value;
        var tablesDisplayValue = totalTables.Value;
        var tablesInSchemaDisplayValue = totalTablesInSchema.Value;
        
        // Build metrics list - add schema tables metric only when schema is selected
        var metricsList = new List<object>
        {
            new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(databasesDisplayValue.ToString("N0"))
            ).Title(currentHasDatabase ? $"Databases: {currentSelectedDb}" : "Databases").Icon(Icons.Database)
                .Key($"databases-{totalDatabases.Value}-{currentSelectedDb ?? "none"}"),
            new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(schemasDisplayValue.ToString("N0"))
            ).Title(currentHasDatabase ? $"Schemas in {currentSelectedDb}" : "Schemas").Icon(Icons.Layers)
                .Key($"schemas-{totalSchemas.Value}-{totalSchemasAll.Value}-{currentSelectedDb ?? "all"}"),
            new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(tablesDisplayValue.ToString("N0"))
            ).Title(currentHasDatabase ? $"Tables in {currentSelectedDb}" : "Tables").Icon(Icons.Table)
                .Key($"tables-{totalTables.Value}-{totalTablesAll.Value}-{currentSelectedDb ?? "all"}")
        };
        
        // Add schema tables metric when schema is selected
        if (currentHasSchema)
        {
            metricsList.Add(
                new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Layout.Horizontal().Gap(2).Align(Align.Center)
                            | Text.H3(tablesInSchemaDisplayValue.ToString("N0"))
                ).Title($"Tables in {currentSelectedSchema}").Icon(Icons.Table)
                    .Key($"tables-in-schema-{totalTablesInSchema.Value}-{currentSelectedSchema}")
            );
        }
        
        // Build stats cards with skeleton during loading
        // Show skeleton if loading stats OR if no data loaded yet (initial state)
        var shouldShowSkeleton = isLoadingStats.Value || databases.Value.Count == 0;
        object statsCards;
        if (shouldShowSkeleton)
        {
            statsCards = BuildStatsSkeletons();
        }
        else
        {
            var layout = Layout.Horizontal().Gap(4).Align(Align.TopCenter);
            foreach (var metric in metricsList)
            {
                layout = layout | metric;
            }
            statsCards = layout;
        }
        
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
            | Text.H2("Database Explorer")
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
        ).Width(Size.Fraction(0.3f));
        
        // Right Section with Tabs
        var rightSection = new Card(
            Layout.Vertical().Gap(4).Padding(3)
            | (hasTable
                ? Layout.Vertical().Gap(3)
                    | Text.H2($"{selectedDatabase.Value}.{selectedSchema.Value}.{selectedTable.Value}")
                    | BuildTabs(activeTab, tableInfo.Value)
                    | (isLoadingTableData.Value 
                        ? BuildSkeletons(7)
                        : activeTab.Value == 0
                            ? (tablePreview.Value?.Rows.Count > 0
                                ? ConvertDataTableToDataTable(tablePreview.Value)
                                : Text.Muted("No data available"))
                            : (tableInfo.Value != null
                                ? Layout.Vertical().Gap(3)
                                    | Text.Muted($"{tableInfo.Value.ColumnCount} columns, {tableInfo.Value.RowCount:N0} rows")
                                    | BuildColumnsTable(tableInfo.Value.Columns)
                                : Text.Muted("No structure information available")))
                : Layout.Vertical().Gap(4)
                    | Text.H2("Table Preview")
                    | Text.Muted("Select a table to preview data"))
                    | (isLoadingData
                        ? BuildSkeletons(3) : new Spacer())

        ).Width(Size.Fraction(0.7f));

        return Layout.Vertical().Gap(4).Padding(4)
            | (Layout.Vertical().Gap(4).Align(Align.TopCenter)
            | Text.H1("Snowflake Database Explorer")
            | Text.Muted("Explore your Snowflake databases, schemas, and tables"))
            | statsCards
            | (errorMessage.Value != null
                ? new Card(
                    Layout.Vertical().Gap(2).Padding(2)
                        | Text.Small($"Error: {errorMessage.Value}")
                )
                : new Spacer())
            | (Layout.Horizontal().Gap(4)
                | leftSection
                | rightSection)
            | (Layout.Vertical().Gap(4).Align(Align.TopCenter)
            | Text.Small("This demo uses Snowflake to explore databases, schemas, and tables.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Snowflake](https://www.snowflake.com/)"))
            ;
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
    
    private object BuildStatsSkeletons()
    {
        // Create skeletons for metrics (usually 3-4 metrics)
        // Use 4 skeletons to cover all possible metrics
        var layout = Layout.Horizontal().Gap(4).Align(Align.TopCenter);
        foreach (var _ in Enumerable.Range(0, 3))
        {
            layout = layout | new Skeleton().Height(Size.Units(50));
        }
        return layout;
    }
    
    private object BuildTabs(IState<int> activeTab, TableInfo? tableInfo)
    {
        var dataTab = new Button("Data")
            .Icon(Icons.Table)
            .Variant(activeTab.Value == 0 ? ButtonVariant.Primary : ButtonVariant.Outline)
            .HandleClick(() => activeTab.Value = 0);
        
        var structureTab = new Button("Structure")
            .Icon(Icons.Layers)
            .Variant(activeTab.Value == 1 ? ButtonVariant.Primary : ButtonVariant.Outline)
            .HandleClick(() => activeTab.Value = 1)
            .Disabled(tableInfo == null);
        
        return Layout.Horizontal().Gap(2)
            | dataTab
            | structureTab;
    }
    
    private object BuildColumnsTable(List<ColumnInfo> columns)
    {
        // Use Key() to force re-render when columns change
        var key = $"columns-{columns.Count}-{string.Join("-", columns.Select(c => c.Name))}";
        return columns.AsQueryable()
            .ToDataTable()
            .Header(c => c.Name, "Column Name")
            .Header(c => c.Type, "Type")
            .Header(c => c.NullableText, "Nullable Text")
            .Width(c => c.Nullable, Size.Px(65))
            .Height(Size.Units(100))
            .Key(key);
    }
    
    private object ConvertDataTableToDataTable(System.Data.DataTable dataTable)
    {
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return Text.Muted("No data available");
        }
        
        var columns = dataTable.Columns.Cast<DataColumn>().ToList();
        var columnCount = Math.Min(columns.Count, 20); // Support up to 20 columns
        
        // Convert DataTable rows to typed DynamicRow records
        var rows = dataTable.Rows.Cast<DataRow>().Select(row =>
        {
            var values = new string[20];
            for (int i = 0; i < columnCount; i++)
            {
                var value = row[columns[i]];
                values[i] = value == DBNull.Value ? "" : value?.ToString() ?? "";
            }
            return new DynamicRow(
                values[0], values[1], values[2], values[3], values[4],
                values[5], values[6], values[7], values[8], values[9],
                values[10], values[11], values[12], values[13], values[14],
                values[15], values[16], values[17], values[18], values[19]
            );
        }).ToList();
        
        // Build DataTable with dynamic headers using reflection
        var builder = rows.AsQueryable().ToDataTable();
        
        // Set headers from actual column names
        for (int i = 0; i < columnCount; i++)
        {
            builder = builder.Header(CreatePropertyExpression<DynamicRow, object>($"C{i}"), columns[i].ColumnName);
        }
        
        // Hide unused columns
        for (int i = columnCount; i < 20; i++)
        {
            builder = builder.Hidden(CreatePropertyExpression<DynamicRow, object>($"C{i}"));
        }
        
        return builder
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;
            })
            .Height(Size.Units(190))
            .Key($"datatable-{dataTable.TableName}-{columns.Count}-{dataTable.Rows.Count}");
    }
    
    // Helper method to create property access expression dynamically
    private static Expression<Func<T, TResult>> CreatePropertyExpression<T, TResult>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(T), "r");
        var property = Expression.Property(parameter, propertyName);
        // Convert to object if TResult is object (for string properties)
        var converted = typeof(TResult) == typeof(object) && property.Type != typeof(object)
            ? Expression.Convert(property, typeof(object))
            : (Expression)property;
        return Expression.Lambda<Func<T, TResult>>(converted, parameter);
    }
}

// Typed record for DataTable widget (supports up to 20 columns)
public record DynamicRow(
    string C0, string C1, string C2, string C3, string C4,
    string C5, string C6, string C7, string C8, string C9,
    string C10, string C11, string C12, string C13, string C14,
    string C15, string C16, string C17, string C18, string C19
);
