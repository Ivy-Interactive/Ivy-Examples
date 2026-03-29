using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IvyAskStatistics.Connections;

public class AppConnection : IConnection
{
    public string GetName() => "IvyAskDb";

    public string GetNamespace() => typeof(AppConnection).Namespace!;

    public string GetConnectionType() => "EntityFramework.PostgreSQL";

    public string GetContext(string connectionPath) => "";

    public ConnectionEntity[] GetEntities()
    {
        return typeof(AppDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => new ConnectionEntity(
                p.PropertyType.GenericTypeArguments[0].Name,
                p.Name))
            .ToArray();
    }

    public Task<(bool ok, string? message)> TestConnection(IConfiguration configuration)
    {
        try
        {
            var cs = configuration["DB_CONNECTION_STRING"];
            if (string.IsNullOrWhiteSpace(cs))
                return Task.FromResult<(bool, string?)>((false, "DB_CONNECTION_STRING not set in user-secrets"));

            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options;
            using var ctx = new AppDbContext(options);
            var canConnect = ctx.Database.CanConnect();
            return Task.FromResult<(bool, string?)>(canConnect
                ? (true, null)
                : (false, "Cannot connect to database"));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(bool, string?)>((false, ex.Message));
        }
    }

    public void RegisterServices(Server server)
    {
        server.Services.AddSingleton<AppDbContextFactory>();
    }

    public Secret[] GetSecrets()
    {
        return [new Secret("DB_CONNECTION_STRING", "PostgreSQL connection string (Supabase)")];
    }
}
