namespace AutodealerCrm.Connections.AutodealerCrm;

public class AutodealerCrmConnection : IConnection, IHaveSecrets
{
    public Task<(bool ok, string? message)> TestConnection(IConfiguration configuration)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AutodealerCrmContext>()
                .UseSqlite("Data Source=db.sqlite")
                .Options;
            using var ctx = new AutodealerCrmContext(options);
            return System.Threading.Tasks.Task.FromResult<(bool, string?)>(ctx.Database.CanConnect() ? (true, null) : (false, "Cannot connect to database"));
        }
        catch (Exception ex)
        {
            return System.Threading.Tasks.Task.FromResult<(bool, string?)>((false, ex.Message));
        }
    }

    public string GetContext(string connectionPath)
    {
        var connectionFile = nameof(AutodealerCrmConnection) + ".cs";
        var contextFactoryFile = nameof(AutodealerCrmContextFactory) + ".cs";
        var files = System.IO.Directory.GetFiles(connectionPath, "*.*", System.IO.SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(connectionFile) && !f.EndsWith(contextFactoryFile) && !f.EndsWith("EfmigrationsLock.cs"))
            .Select(System.IO.File.ReadAllText)
            .ToArray();
        return string.Join(System.Environment.NewLine, files);
    }

    public string GetName() => nameof(AutodealerCrm);

    public string GetNamespace() => typeof(AutodealerCrmConnection).Namespace;

    public string GetConnectionType() => "EntityFramework.Sqlite";

    public ConnectionEntity[] GetEntities()
    {
        return typeof(AutodealerCrmContext)
            .GetProperties()
            .Where(e => e.PropertyType.IsGenericType && e.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Where(e => e.PropertyType.GenericTypeArguments[0].Name != "EfmigrationsLock")
            .Select(e => new ConnectionEntity(e.PropertyType.GenericTypeArguments[0].Name, e.Name))
            .ToArray();
    }

    public void RegisterServices(Server server)
    {
        server.Services.AddSingleton<AutodealerCrmContextFactory>();
    }

    public Secret[] GetSecrets()
    {
        return [];
    }
}
