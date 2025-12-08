namespace SnowflakeDashboard.Services;

public class SnowflakeService
{
    private readonly string? _connectionString;
    
    public SnowflakeService(string? connectionString) => _connectionString = connectionString;
    
    public async Task<System.Data.DataTable> ExecuteQueryAsync(string sql)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Snowflake connection is not configured.");
        
        using var connection = new SnowflakeDbConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync();
        var dataTable = new System.Data.DataTable();
        dataTable.Load(reader);
        return dataTable;
    }
    
    public async Task<object?> ExecuteScalarAsync(string sql)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Snowflake connection is not configured.");
        
        using var connection = new SnowflakeDbConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }
}
