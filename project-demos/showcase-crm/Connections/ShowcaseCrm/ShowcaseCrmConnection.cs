using Ivy.Connections;
using Ivy.Services;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

public class ShowcaseCrmConnection : IConnection, IHaveSecrets
{
    public string GetContext(string connectionPath)
    {
        var connectionFile = nameof(ShowcaseCrmConnection) + ".cs";
        var contextFactoryFile = nameof(ShowcaseCrmContextFactory) + ".cs";
        var files = System.IO.Directory.GetFiles(connectionPath, "*.*", System.IO.SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(connectionFile) && !f.EndsWith(contextFactoryFile) && !f.EndsWith("EfmigrationsLock.cs"))
            .Select(System.IO.File.ReadAllText)
            .ToArray();
        return string.Join(System.Environment.NewLine, files);
    }

    public string GetName() => nameof(ShowcaseCrm);

    public string GetNamespace() => typeof(ShowcaseCrmConnection).Namespace;

    public string GetConnectionType() => "EntityFramework.Sqlite";

    public ConnectionEntity[] GetEntities()
    {
        return typeof(ShowcaseCrmContext)
            .GetProperties()
            .Where(e => e.PropertyType.IsGenericType && e.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Where(e => e.PropertyType.GenericTypeArguments[0].Name != "EfmigrationsLock")
            .Select(e => new ConnectionEntity(e.PropertyType.GenericTypeArguments[0].Name, e.Name))
            .ToArray();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ShowcaseCrmContextFactory>();
    }

   public Ivy.Services.Secret[] GetSecrets()
   {
       return
       [
           
       ];
   }
}
