using System.Data;
using Snowflake.Data.Client;

namespace SnowflakeExample.Services;

public class TableInfo
{
    public string Database { get; set; } = "";
    public string Schema { get; set; } = "";
    public string Table { get; set; } = "";
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
    public List<ColumnInfo> Columns { get; set; } = new();
}

public class ColumnInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Nullable { get; set; }
}

/// <summary>
/// Service for executing SQL queries against Snowflake database
/// </summary>
public class SnowflakeService
{
    private readonly string _connectionString;
    
    public SnowflakeService(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    /// <summary>
    /// Execute a SQL query and return results as DataTable
    /// </summary>
    public async Task<System.Data.DataTable> ExecuteQueryAsync(string sql)
    {
        using var connection = new SnowflakeDbConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync();
        var dataTable = new System.Data.DataTable();
        dataTable.Load(reader);
        
        return dataTable;
    }
    
    /// <summary>
    /// Execute a SQL query and return scalar value
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(string sql)
    {
        using var connection = new SnowflakeDbConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        return await command.ExecuteScalarAsync();
    }
    
    /// <summary>
    /// Get list of all databases
    /// </summary>
    public async Task<List<string>> GetDatabasesAsync()
    {
        var sql = "SHOW DATABASES";
        var dataTable = await ExecuteQueryAsync(sql);
        
        var databases = new List<string>();
        foreach (DataRow row in dataTable.Rows)
        {
            var dbName = row["name"]?.ToString();
            if (!string.IsNullOrEmpty(dbName))
            {
                databases.Add(dbName);
            }
        }
        
        return databases;
    }
    
    /// <summary>
    /// Get list of available schemas in a database
    /// </summary>
    public async Task<List<string>> GetSchemasAsync(string? database = null)
    {
        var sql = database != null 
            ? $"SHOW SCHEMAS IN DATABASE {database}"
            : "SHOW SCHEMAS";
        var dataTable = await ExecuteQueryAsync(sql);
        
        var schemas = new List<string>();
        foreach (DataRow row in dataTable.Rows)
        {
            var schemaName = row["name"]?.ToString();
            if (!string.IsNullOrEmpty(schemaName))
            {
                schemas.Add(schemaName);
            }
        }
        
        return schemas;
    }
    
    /// <summary>
    /// Get list of tables in a schema
    /// </summary>
    public async Task<List<string>> GetTablesAsync(string database, string schema)
    {
        var sql = $"SHOW TABLES IN SCHEMA {database}.{schema}";
        var dataTable = await ExecuteQueryAsync(sql);
        
        var tables = new List<string>();
        foreach (DataRow row in dataTable.Rows)
        {
            var tableName = row["name"]?.ToString();
            if (!string.IsNullOrEmpty(tableName))
            {
                tables.Add(tableName);
            }
        }
        
        return tables;
    }
    
    /// <summary>
    /// Get table information including row count and columns
    /// </summary>
    public async Task<TableInfo> GetTableInfoAsync(string database, string schema, string table)
    {
        var fullTableName = $"{database}.{schema}.{table}";
        
        // Get row count
        var countSql = $"SELECT COUNT(*) as ROW_COUNT FROM {fullTableName}";
        var countResult = await ExecuteScalarAsync(countSql);
        var rowCount = countResult != null ? Convert.ToInt64(countResult) : 0;
        
        // Get columns
        var columnsSql = $"DESCRIBE TABLE {fullTableName}";
        var columnsTable = await ExecuteQueryAsync(columnsSql);
        
        var columns = new List<ColumnInfo>();
        foreach (DataRow row in columnsTable.Rows)
        {
            columns.Add(new ColumnInfo
            {
                Name = row["name"]?.ToString() ?? "",
                Type = row["type"]?.ToString() ?? "",
                Nullable = row["null?"]?.ToString()?.ToUpper() == "Y"
            });
        }
        
        return new TableInfo
        {
            Database = database,
            Schema = schema,
            Table = table,
            RowCount = rowCount,
            ColumnCount = columns.Count,
            Columns = columns
        };
    }
    
    /// <summary>
    /// Get preview data from a table (first N rows)
    /// </summary>
    public async Task<System.Data.DataTable> GetTablePreviewAsync(string database, string schema, string table, int limit = 1000)
    {
        var fullTableName = $"{database}.{schema}.{table}";
        var sql = $"SELECT * FROM {fullTableName} LIMIT {limit}";
        return await ExecuteQueryAsync(sql);
    }
    
    /// <summary>
    /// Test connection to Snowflake
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var result = await ExecuteScalarAsync("SELECT 1");
            return result != null;
        }
        catch
        {
            return false;
        }
    }
}

