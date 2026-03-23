using Microsoft.EntityFrameworkCore.Design;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

public class ShowcaseCrmDesignTimeContextFactory : IDesignTimeDbContextFactory<ShowcaseCrmContext>
{
    public ShowcaseCrmContext CreateDbContext(string[] args)
    {
        var serverArgs = new ServerArgs { Verbose = false };
        var contextFactory = new ShowcaseCrmContextFactory(serverArgs);
        return contextFactory.CreateDbContext();
    }
}
