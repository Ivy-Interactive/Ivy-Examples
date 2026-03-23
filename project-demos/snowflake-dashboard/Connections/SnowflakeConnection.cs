namespace SnowflakeDashboard.Connections;

public class SnowflakeConnection : IConnection
{
    public SnowflakeConnection() { }
    
    public string GetConnectionType() => typeof(SnowflakeConnection).ToString();
    public string GetContext(string connectionPath) => throw new NotImplementedException();
    public ConnectionEntity[] GetEntities() => throw new NotImplementedException();
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
            var connStr = $"account={account};user={user};password={password};";
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
        server.Services.AddSingleton<SnowflakeConnection>(this);
        server.Services.AddScoped<SnowflakeService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var account = configuration["Snowflake:Account"];
            var user = configuration["Snowflake:User"];
            var password = configuration["Snowflake:Password"];
            
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                return new SnowflakeService("");
            }
            
            var connString = $"account={account};user={user};password={password};";
            return new SnowflakeService(connString);
        });
    }
    
    public string GetConnectionString(IConfiguration configuration) => "";
}
