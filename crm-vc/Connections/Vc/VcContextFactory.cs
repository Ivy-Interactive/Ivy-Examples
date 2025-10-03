using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using Ivy.Services;
using Path = System.IO.Path;

namespace Vc.Connections.Vc;

public sealed class VcContextFactory : IDbContextFactory<VcContext>
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    private readonly ServerArgs _args;
    private readonly string _absolutePath;
    private readonly string _relativePath = "db.sqlite";
    private readonly string _uniqueId = "jdfh3f8e";
    private readonly ILogger? _logger;

    public VcContextFactory(
        ServerArgs args,
        IVolume? volume = null,
        ILogger? logger = null
    )
    {
        _args = args;
        var volume1 = volume ?? new FolderVolume(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ivy-Data", nameof(Vc)));
        _absolutePath = volume1.GetAbsolutePath(_uniqueId + "." + _relativePath);
        _logger = logger;
    }

    public VcContext CreateDbContext()
    {
        EnsureDatabasePresentOnce();

        var optionsBuilder = new DbContextOptionsBuilder<VcContext>()
            .UseSqlite($"Data Source=\"{_absolutePath}\"");

        if (_args.Verbose)
        {
            optionsBuilder
                .EnableSensitiveDataLogging()
                .LogTo(s => _logger?.LogInformation("{EFLog}", s), LogLevel.Information);
        }

        return new VcContext(optionsBuilder.Options);
    }

    private void EnsureDatabasePresentOnce()
    {
        if (File.Exists(_absolutePath)) return;

        InitLock.Wait();
        try
        {
            if (File.Exists(_absolutePath)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(_absolutePath)!);

            var appDir = AppContext.BaseDirectory;
            var templatePath = Path.Combine(appDir, _relativePath);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Database template not found at '{templatePath}'. Make sure 'db.sqlite' is copied to the output folder.");
            }

            var tmp = _absolutePath + ".tmp";
            File.Copy(templatePath, tmp, overwrite: true);
            File.Move(tmp, _absolutePath);
            _logger?.LogInformation("Initialized persistent database at '{Path}'.", _absolutePath);
        }
        catch (IOException)
        {
            if (!File.Exists(_absolutePath)) throw;
        }
        finally
        {
            InitLock.Release();
        }
    }
}