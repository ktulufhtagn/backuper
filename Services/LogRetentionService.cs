using Backuper.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

public sealed class LogRetentionService(
    IBackupPathProvider pathProvider,
    IOptions<BackupOptions> options,
    ILogger<LogRetentionService> logger) : ILogRetentionService
{
    public void DeleteExpiredLogs()
    {
        var backupOptions = options.Value;
        var logDirectory = pathProvider.LogDirectory;

        if (!Directory.Exists(logDirectory))
        {
            return;
        }

        var cutoffUtc = DateTime.UtcNow.AddDays(-backupOptions.LogRetentionDays);

        foreach (var logFile in Directory.EnumerateFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var lastWriteTimeUtc = File.GetLastWriteTimeUtc(logFile);
                if (lastWriteTimeUtc >= cutoffUtc)
                {
                    continue;
                }

                File.Delete(logFile);
                logger.LogInformation("Deleted expired backup log {LogFile}.", logFile);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not delete expired backup log {LogFile}.", logFile);
            }
        }
    }
}
