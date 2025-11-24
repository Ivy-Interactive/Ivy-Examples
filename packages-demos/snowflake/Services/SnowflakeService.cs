using System.Data;
using Snowflake.Data.Client;

namespace SnowflakeExample.Services;

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
    /// Get list of available schemas in SNOWFLAKE_SAMPLE_DATA
    /// </summary>
    public async Task<List<string>> GetSchemasAsync()
    {
        var sql = "SHOW SCHEMAS IN DATABASE SNOWFLAKE_SAMPLE_DATA";
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
    public async Task<List<string>> GetTablesAsync(string schema)
    {
        var sql = $"SHOW TABLES IN SCHEMA SNOWFLAKE_SAMPLE_DATA.{schema}";
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

