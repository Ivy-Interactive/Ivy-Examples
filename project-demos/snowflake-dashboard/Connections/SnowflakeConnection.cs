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
            if (VerifiedCredentials.IsVerified && VerifiedCredentials.HasCredentials)
            {
                var connString = $"account={VerifiedCredentials.Account};user={VerifiedCredentials.User};password={VerifiedCredentials.Password};";
                return new SnowflakeService(connString);
            }
            return new SnowflakeService("");
        });
    }
    
    public string GetConnectionString(IConfiguration configuration) => "";
}
