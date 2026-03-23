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

    public Task<(bool ok, string? message)> TestConnection(IConfiguration configuration)
    {
        var account = configuration["Snowflake:Account"];
        var user = configuration["Snowflake:User"];
        var password = configuration["Snowflake:Password"];
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return System.Threading.Tasks.Task.FromResult<(bool, string?)>((false, "Snowflake credentials not configured (Account, User, Password)"));

        try
        {
            var connStr = GetConnectionString(configuration, account, user, password);
            using var connection = new Snowflake.Data.Client.SnowflakeDbConnection(connStr);
            connection.Open();
            return System.Threading.Tasks.Task.FromResult<(bool, string?)>((true, null));
        }
        catch (Exception ex)
        {
            return System.Threading.Tasks.Task.FromResult<(bool, string?)>((false, ex.Message));
        }
    }

    public void RegisterServices(Server server)
    {
        // Register this connection as singleton
        server.Services.AddSingleton<SnowflakeConnection>(this);

        // SnowflakeService will be created in components with credentials from UseState
        // Register as scoped but with empty connection string - components will create their own instances
        server.Services.AddScoped<SnowflakeService>(sp => new SnowflakeService(""));
    }

    public string GetConnectionString(IConfiguration configuration, string account, string user, string password)
    {
        var warehouse = configuration["Snowflake:Warehouse"] ?? "";
        var database = configuration["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
        var schema = configuration["Snowflake:Schema"] ?? "TPCH_SF1";

        // Build connection string
        return $"account={account};user={user};password={password};warehouse={warehouse};db={database};schema={schema};";
    }
}

