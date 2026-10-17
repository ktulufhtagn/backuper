using Backuper.Configuration;
using Backuper.Services;
using Microsoft.Extensions.Options;

namespace Backuper;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<BackupOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IBackupScheduler>();
                await scheduler.RunIfDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Backup scheduler iteration failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(options.Value.CheckIntervalMinutes), stoppingToken);
        }
    }
}
