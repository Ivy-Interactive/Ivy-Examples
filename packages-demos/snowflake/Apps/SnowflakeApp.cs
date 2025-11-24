using System.Data;
using SnowflakeExample.Services;

namespace SnowflakeExample.Apps;

/// <summary>
/// Snowflake Demo App - Interactive interface for querying SNOWFLAKE_SAMPLE_DATA
/// </summary>
[App(icon: Icons.Database, title: "Snowflake Sample Data Explorer")]
public class SnowflakeApp : ViewBase, IHaveSecrets
{
    public override object? Build()
    {
        var snowflakeService = this.UseService<SnowflakeService>();
        var refreshToken = this.UseRefreshToken();
        
        // State management
        var selectedSchema = this.UseState("TPCH_SF1");
        var selectedTable = this.UseState<string?>(() => null);
        var customQuery = this.UseState("");
        var queryResult = this.UseState<System.Data.DataTable?>(() => null);
        var errorMessage = this.UseState<string?>(() => null);
        var isLoading = this.UseState(false);
        
        // Predefined sample queries for SNOWFLAKE_SAMPLE_DATA
        var sampleQueries = new[]
        {
            ("Top 10 Customers by Revenue", 
             "SELECT C_NAME, C_ACCTBAL, C_PHONE, C_MKTSEGMENT " +
             "FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.CUSTOMER " +
             "ORDER BY C_ACCTBAL DESC " +
             "LIMIT 10"),
            
            ("Orders Summary", 
             "SELECT O_ORDERSTATUS, COUNT(*) as ORDER_COUNT, SUM(O_TOTALPRICE) as TOTAL_REVENUE " +
             "FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.ORDERS " +
             "GROUP BY O_ORDERSTATUS " +
             "ORDER BY TOTAL_REVENUE DESC"),
            
            ("Top Products by Quantity", 
             "SELECT L_PARTKEY, SUM(L_QUANTITY) as TOTAL_QUANTITY, SUM(L_EXTENDEDPRICE) as TOTAL_PRICE " +
             "FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.LINEITEM " +
             "GROUP BY L_PARTKEY " +
             "ORDER BY TOTAL_QUANTITY DESC " +
             "LIMIT 20"),
            
            ("Customer Orders Count", 
             "SELECT C.C_NAME, COUNT(O.O_ORDERKEY) as ORDER_COUNT " +
             "FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.CUSTOMER C " +
             "LEFT JOIN SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.ORDERS O ON C.C_CUSTKEY = O.O_CUSTKEY " +
             "GROUP BY C.C_NAME " +
             "ORDER BY ORDER_COUNT DESC " +
             "LIMIT 15"),
            
            ("Nation Statistics", 
             "SELECT N_NAME, COUNT(DISTINCT C_CUSTKEY) as CUSTOMER_COUNT " +
             "FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.NATION N " +
             "JOIN SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.CUSTOMER C ON N.N_NATIONKEY = C.C_NATIONKEY " +
             "GROUP BY N_NAME " +
             "ORDER BY CUSTOMER_COUNT DESC")
        };
        
        var selectedSampleQuery = this.UseState(0);
        
        // Execute query handler
        async Task ExecuteQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                errorMessage.Value = "Please enter a SQL query";
                return;
            }
            
            isLoading.Value = true;
            errorMessage.Value = null;
            queryResult.Value = null;
            refreshToken.Refresh();
            
            try
            {
                var result = await snowflakeService.ExecuteQueryAsync(sql);
                queryResult.Value = result;
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error executing query: {ex.Message}";
            }
            finally
            {
                isLoading.Value = false;
                refreshToken.Refresh();
            }
        }
        
        // Sample schemas available in SNOWFLAKE_SAMPLE_DATA
        var availableSchemas = new[] { "TPCH_SF1", "TPCH_SF10", "TPCH_SF100", "TPCDS_SF10TCL", "WEATHER" };
        
        // Left Panel - Query Builder
        var schemaDropdown = new Button(selectedSchema.Value)
            .Primary()
            .Icon(Icons.Database)
            .WithDropDown(availableSchemas.Select(schema => 
                MenuItem.Default(schema).HandleSelect(() => 
                {
                    selectedSchema.Value = schema;
                    selectedTable.Value = null;
                    queryResult.Value = null;
                    refreshToken.Refresh();
                })
            ).ToArray());
        
        var sampleQueryDropdown = new Button("Sample Queries")
            .Secondary()
            .Icon(Icons.FileText)
            .WithDropDown(sampleQueries.Select((query, idx) =>
                MenuItem.Default(query.Item1).HandleSelect(() =>
                {
                    selectedSampleQuery.Value = idx;
                    customQuery.Value = query.Item2;
                    refreshToken.Refresh();
                })
            ).ToArray());
        
        var executeButton = new Button("Execute Query")
        {
            OnClick = async (evt) =>
            {
                await ExecuteQuery(customQuery.Value);
                await ValueTask.CompletedTask;
            }
        }
            .Primary()
            .Icon(Icons.Play);
        
        var queryTextArea = customQuery.ToTextAreaInput()
            .Placeholder("Enter your SQL query here...\nExample: SELECT * FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.CUSTOMER LIMIT 10")
            .Height(Size.Units(120));
        
        var leftPanel = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Query Builder")
            | Text.Muted("Query SNOWFLAKE_SAMPLE_DATA database")
            | Layout.Horizontal().Gap(2)
                | schemaDropdown
                | sampleQueryDropdown
            | queryTextArea
            | Layout.Horizontal().Gap(2)
                | executeButton
                | (isLoading.Value ? Text.Small("Executing...") : new Spacer())
            | (errorMessage.Value != null 
                ? Text.Small(errorMessage.Value) 
                : new Spacer())
        ).Width(Size.Fraction(0.45f));
        
        // Right Panel - Results
        var resultsContent = queryResult.Value != null && queryResult.Value.Columns.Count > 0
            ? BuildDataTable(queryResult.Value)
            : Text.Muted("No results. Execute a query to see data.");
        
        var rightPanel = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Query Results")
            | Text.Muted(queryResult.Value != null 
                ? $"Showing {queryResult.Value.Rows.Count} rows" 
                : "Results will appear here")
            | resultsContent
        ).Width(Size.Fraction(0.45f)).Height(Size.Fit().Min(400));
        
        return Layout.Horizontal().Gap(6).Align(Align.Center)
            | leftPanel
            | rightPanel;
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

