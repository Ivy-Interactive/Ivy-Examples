using Ivy.Connections;
using Snowflake.Data.Client;
using SnowflakeExample.Services;

namespace SnowflakeExample.Connections;

/// <summary>
/// Connection to Snowflake database using SNOWFLAKE_SAMPLE_DATA
/// </summary>
public class SnowflakeConnection : IConnection
{
    public SnowflakeConnection()
    {
        // Parameterless constructor required by Ivy framework
    }
    
    public string GetConnectionType()
    {
        return typeof(SnowflakeConnection).ToString();
    }

    public string GetContext(string connectionPath)
    {
        throw new NotImplementedException();
    }

    public ConnectionEntity[] GetEntities()
    {
        throw new NotImplementedException();
    }

    public string GetName() => nameof(SnowflakeConnection);

    public string GetNamespace() => typeof(SnowflakeConnection).Namespace ?? "";

    public void RegisterServices(IServiceCollection services)
    {
        // Register this connection as singleton
        services.AddSingleton<SnowflakeConnection>(this);
        
        // Register SnowflakeService with connection string built from configuration
        services.AddSingleton<SnowflakeService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            // Get connection parameters from configuration or use defaults
            var account = configuration["Snowflake:Account"] ?? "";
            var user = configuration["Snowflake:User"] ?? "";
            var password = configuration["Snowflake:Password"] ?? "";
            var warehouse = configuration["Snowflake:Warehouse"] ?? "";
            var database = configuration["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
            var schema = configuration["Snowflake:Schema"] ?? "TPCH_SF1";
            
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(account))
            {
                throw new InvalidOperationException(
                    "Snowflake Account is not configured. Please set 'Snowflake:Account' in appsettings.json or environment variables.");
            }
            
            if (string.IsNullOrWhiteSpace(user))
            {
                throw new InvalidOperationException(
                    "Snowflake User is not configured. Please set 'Snowflake:User' in appsettings.json or environment variables.");
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Snowflake Password is not configured. Please set 'Snowflake:Password' in appsettings.json or environment variables.");
            }
            
            // Build connection string
            // Note: Account should be in format like "ab12345.us-west-2" (without snowflakecomputing.com)
            var connectionString = $"account={account};user={user};password={password};warehouse={warehouse};db={database};schema={schema};";
            
            return new SnowflakeService(connectionString);
        });
    }
    
    public string GetConnectionString(IConfiguration configuration)
    {
        // Get connection parameters from configuration or use defaults
        var account = configuration["Snowflake:Account"] ?? "";
        var user = configuration["Snowflake:User"] ?? "";
        var password = configuration["Snowflake:Password"] ?? "";
        var warehouse = configuration["Snowflake:Warehouse"] ?? "";
        var database = configuration["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
        var schema = configuration["Snowflake:Schema"] ?? "TPCH_SF1";
        
        // Build connection string
        return $"account={account};user={user};password={password};warehouse={warehouse};db={database};schema={schema};";
    }
}

