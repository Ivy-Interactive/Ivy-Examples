namespace SnowflakeDashboard.Connections;

public class SnowflakeConnection : IConnection
{
    public SnowflakeConnection() { }
    
    public string GetConnectionType() => typeof(SnowflakeConnection).ToString();
    public string GetContext(string connectionPath) => throw new NotImplementedException();
    public ConnectionEntity[] GetEntities() => throw new NotImplementedException();
    public string GetName() => nameof(SnowflakeConnection);
    public string GetNamespace() => typeof(SnowflakeConnection).Namespace ?? "";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<SnowflakeConnection>(this);
        services.AddScoped<SnowflakeService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var account = config["Snowflake:Account"] ?? "";
            var user = config["Snowflake:User"] ?? "";
            var password = config["Snowflake:Password"] ?? "";
            
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
                return new SnowflakeService("");
            
            var connString = $"account={account};user={user};password={password};";
            return new SnowflakeService(connString);
        });
    }
    
    public string GetConnectionString(IConfiguration configuration) => "";
}
