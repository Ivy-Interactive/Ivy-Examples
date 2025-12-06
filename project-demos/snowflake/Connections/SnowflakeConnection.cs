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
        
        // Register SnowflakeService as scoped so it gets recreated with updated credentials
        // This ensures it always uses the latest VerifiedCredentials when they are updated
        services.AddScoped<SnowflakeService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            // Priority: VerifiedCredentials > Environment Variables > Configuration
            var account = VerifiedCredentials.Account ?? configuration["Snowflake:Account"] ?? "";
            var user = VerifiedCredentials.User ?? configuration["Snowflake:User"] ?? "";
            var password = VerifiedCredentials.Password ?? configuration["Snowflake:Password"] ?? "";
            var warehouse = configuration["Snowflake:Warehouse"] ?? "";
            var database = configuration["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
            var schema = configuration["Snowflake:Schema"] ?? "TPCH_SF1";
            
            // Only validate if we have credentials (don't throw if credentials are being entered)
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                // Return a service with empty connection string - it will fail gracefully when used
                return new SnowflakeService("");
            }
            
            // Build connection string
            // Note: Account should be in format like "ab12345.us-west-2" (without snowflakecomputing.com)
            var connectionString = $"account={account};user={user};password={password};warehouse={warehouse};db={database};schema={schema};";
            
            return new SnowflakeService(connectionString);
        });
    }
    
    public string GetConnectionString(IConfiguration configuration)
    {
        // Priority: VerifiedCredentials > Environment Variables > Configuration
        var account = VerifiedCredentials.Account ?? configuration["Snowflake:Account"] ?? "";
        var user = VerifiedCredentials.User ?? configuration["Snowflake:User"] ?? "";
        var password = VerifiedCredentials.Password ?? configuration["Snowflake:Password"] ?? "";
        var warehouse = configuration["Snowflake:Warehouse"] ?? "";
        var database = configuration["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
        var schema = configuration["Snowflake:Schema"] ?? "TPCH_SF1";
        
        // Build connection string
        return $"account={account};user={user};password={password};warehouse={warehouse};db={database};schema={schema};";
    }
}

