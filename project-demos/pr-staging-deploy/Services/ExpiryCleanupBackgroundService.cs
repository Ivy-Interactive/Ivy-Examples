namespace PrStagingDeploy.Services;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Background job: every hour, delete staging deployments older than ExpiryDays.
/// </summary>
public class ExpiryCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiryCleanupBackgroundService> _logger;

    public ExpiryCleanupBackgroundService(IServiceProvider services, ILogger<ExpiryCleanupBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                using var scope = _services.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var apiToken = config["Sliplane:ApiToken"];
                if (string.IsNullOrEmpty(apiToken))
                    continue;

                var deployService = scope.ServiceProvider.GetRequiredService<StagingDeployService>();
                var result = await deployService.DeleteExpiredAsync(apiToken);
                if (result.Success)
                    _logger.LogInformation("Expiry cleanup: {Message}", result.Message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiry cleanup failed");
            }
        }
    }
}
