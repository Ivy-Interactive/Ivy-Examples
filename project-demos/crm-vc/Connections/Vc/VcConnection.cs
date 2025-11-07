using Ivy.Connections;

namespace Vc.Connections.Vc;

public class VcConnection : IConnection
{
    public string GetContext(string connectionPath)
    {
        var connectionFile = nameof(VcConnection) + ".cs";
        var contextFactoryFile = nameof(VcContextFactory) + ".cs";
        var files = Directory.GetFiles(connectionPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(connectionFile) && !f.EndsWith(contextFactoryFile))
            .Select(File.ReadAllText)
            .ToArray();
        return string.Join(Environment.NewLine, files);
    }

    public string GetName() => nameof(Vc);

    public string GetNamespace() => typeof(VcConnection).Namespace;
    
    public string GetConnectionType() => "EntityFramework.Sqlite";
    
    public ConnectionEntity[] GetEntities()
    {
        return typeof(VcContext)
            .GetProperties()
            .Where(e => e.PropertyType.IsGenericType && e.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(e => new ConnectionEntity(e.PropertyType.GenericTypeArguments[0].Name, e.Name))
            .ToArray();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<VcContextFactory>();
    }
}