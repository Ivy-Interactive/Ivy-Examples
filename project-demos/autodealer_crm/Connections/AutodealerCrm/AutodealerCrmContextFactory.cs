using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using Ivy.Services;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutodealerCrm.Connections.AutodealerCrm;

public sealed class AutodealerCrmContextFactory : IDbContextFactory<AutodealerCrmContext>
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    private readonly ServerArgs _args;
    private readonly string _absolutePath;
    private readonly string _relativePath = "db.sqlite";
    private readonly string _uniqueId = "3f5550ae";
    private readonly ILogger? _logger;

    public AutodealerCrmContextFactory(
        ServerArgs args,
        IVolume? volume = null,
        ILogger? logger = null
    )
    {
        _args = args;
        var volume1 = volume ?? new FolderVolume(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ivy-Data", "AutodealerCrm"));
        _absolutePath = volume1.GetAbsolutePath(_uniqueId + "." + _relativePath);
        _logger = logger;
    }

    public AutodealerCrmContext CreateDbContext()
    {
        EnsureDatabasePresentOnce();

        var optionsBuilder = new DbContextOptionsBuilder<AutodealerCrmContext>()
            .UseSqlite($@"Data Source=""{_relativePath}""");

        if (_args.Verbose)
        {
            optionsBuilder
                .EnableSensitiveDataLogging()
                .LogTo(s => _logger?.LogInformation("{EFLog}", s), LogLevel.Information);
        }

        return new AutodealerCrmContext(optionsBuilder.Options);
    }

    private void EnsureDatabasePresentOnce()
    {
        if (System.IO.File.Exists(_absolutePath)) return;

        InitLock.Wait();
        try
        {
            if (System.IO.File.Exists(_absolutePath)) return;

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_absolutePath)!);

            var appDir = AppContext.BaseDirectory;
            var templatePath = System.IO.Path.Combine(appDir, _relativePath);

            if (!System.IO.File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Database template not found at '{templatePath}'. Make sure '{_relativePath}' is copied to the output folder.");
            }

            var tmp = _absolutePath + ".tmp";
            System.IO.File.Copy(templatePath, tmp, overwrite: true);
            System.IO.File.Move(tmp, _absolutePath);
            _logger?.LogInformation("Initialized persistent database at '{Path}'.", _absolutePath);
        }
        catch (IOException)
        {
            if (!System.IO.File.Exists(_absolutePath)) throw;
        }
        finally
        {
            InitLock.Release();
        }
    }
}
