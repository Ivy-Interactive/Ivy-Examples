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
    
    public async Task<bool> TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return false;
        
        SnowflakeDbConnection? connection = null;
        try
        {
            var testConnectionString = _connectionString.TrimEnd(';') + ";poolingEnabled=false;";
            connection = new SnowflakeDbConnection(testConnectionString);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await connection.OpenAsync(cts.Token);
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            
            return await command.ExecuteScalarAsync(cts.Token) != null;
        }
        catch
        {
            return false;
        }
        finally
        {
            try
            {
                if (connection?.State == System.Data.ConnectionState.Open)
                    connection.Close();
                connection?.Dispose();
            }
            catch { }
        }
    }
}
