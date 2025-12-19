using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutodealerCrm;

public class DesignFactory
    : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("No arguments provided. Connection string argument is required.");
        }

        string connectionString = args[0];
        var provider = DatabaseProviderFactory.Create(DatabaseProvider.Sqlite);
        return provider.GetDbContext<DataContext>(connectionString);
    }
}
