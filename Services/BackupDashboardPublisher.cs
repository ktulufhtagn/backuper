using System.Text.Json;
using Backuper.Configuration;
using Backuper.Data;
using Backuper.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

public sealed class BackupDashboardPublisher(
    IServiceScopeFactory scopes, NotificationQueue queue, IOptions<BackupOptions> options,
    ILogger<BackupDashboardPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = new RcloneStatisticsReader();
        var path = Path.Combine(queue.DirectoryPath, "Dashboard", "status.json");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BackupDbContext>();
                var latest = await db.BackupExecutions.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                var counts = await db.BackupExecutions.GroupBy(x => x.Status)
                    .Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync(stoppingToken);
                var config = options.Value;
                TransferStatistics? stats = null;
                try { stats = reader.Read(latest?.LogFilePath); }
                catch (IOException) { /* A rotating or temporarily locked log must not hide service status. */ }
                var snapshot = new BackupDashboard(DateTimeOffset.UtcNow, latest?.Status ?? "Idle",
                    latest?.StartedAtLocal, latest?.CompletedAtLocal, latest?.ErrorMessage, stats,
                    config.SourcePath, config.Destination, config.UploadBandwidthLimit, config.DailyRunTime,
                    config.Transfers, counts.FirstOrDefault(x => x.Status == "Succeeded")?.Count ?? 0,
                    counts.FirstOrDefault(x => x.Status == "Failed")?.Count ?? 0, latest?.LogFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(snapshot), stoppingToken);
                File.Move(path + ".tmp", path, overwrite: true);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not publish backup dashboard status.");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
