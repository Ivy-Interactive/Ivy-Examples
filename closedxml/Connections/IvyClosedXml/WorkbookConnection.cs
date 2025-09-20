using Ivy.Connections;


/// <summary>
/// Implement IConnection interface so that WorkbookConnection is registered in the DI container
/// </summary>
public class WorkbookConnection : IConnection
{
    private WorkbookRepository excelFileRepository { get; set; }

    public WorkbookConnection()
    {
        excelFileRepository = new WorkbookRepository();
    }

    public WorkbookRepository GetWorkbookRepository()
    {
        return excelFileRepository;
    }

    public string GetConnectionType()
    {
        return typeof(WorkbookConnection).ToString();
    }

    public string GetContext(string connectionPath)
    {
        throw new NotImplementedException();
    }

    public ConnectionEntity[] GetEntities()
    {
        throw new NotImplementedException();
    }

    public string GetName() => nameof(WorkbookConnection);

    public string GetNamespace() => typeof(WorkbookConnection).Namespace;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<WorkbookConnection>();
    }
}