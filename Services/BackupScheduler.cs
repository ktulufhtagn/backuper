using System.Globalization;
using Backuper.Configuration;
using Backuper.Data;
using Backuper.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

public sealed class BackupScheduler(
    BackupDbContext database,
    IBackupExecutor backupExecutor,
    ILogRetentionService logRetentionService,
    IOptions<BackupOptions> options,
    ILogger<BackupScheduler> logger) : IBackupScheduler
{
    public async Task RunIfDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var scheduledTime = TimeOnly.Parse(options.Value.DailyRunTime, CultureInfo.InvariantCulture);
        var today = DateOnly.FromDateTime(now.LocalDateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        logRetentionService.DeleteExpiredLogs();

        if (TimeOnly.FromDateTime(now.LocalDateTime) < scheduledTime)
        {
            logger.LogInformation("Backup is not due yet. Scheduled time: {ScheduledTime}", scheduledTime);
            return;
        }

        var backupAlreadySucceeded = await database.BackupExecutions.AnyAsync(
            execution => execution.ScheduledDate == today && execution.Status == BackupStatus.Succeeded,
            cancellationToken);

        if (backupAlreadySucceeded)
        {
            logger.LogInformation("Backup already succeeded for {ScheduledDate}.", today);
            return;
        }

        var backupAlreadyRunning = await database.BackupExecutions.AnyAsync(
            execution => execution.ScheduledDate == today && execution.Status == BackupStatus.InProgress,
            cancellationToken);

        if (backupAlreadyRunning)
        {
            logger.LogInformation("Backup is already running for {ScheduledDate}.", today);
            return;
        }

        logger.LogInformation("Backup is due for {ScheduledDate}. Starting now.", today);
        await backupExecutor.ExecuteAsync(today, cancellationToken);
    }
}
