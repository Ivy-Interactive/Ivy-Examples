using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Vc.Connections.Vc;

public class VcContextFactory(ServerArgs args) : IDbContextFactory<VcContext>
{
    public VcContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<VcContext>();

        optionsBuilder.UseSqlite("Data Source=db.sqlite");

        if (args.Verbose)
        {
            optionsBuilder
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, LogLevel.Information);
        }

        return new VcContext(optionsBuilder.Options);
    }
}