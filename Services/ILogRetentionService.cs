namespace Backuper.Services;

/// <summary>Removes backup logs that exceed the configured retention period.</summary>
public interface ILogRetentionService
{
    /// <summary>Deletes expired log files; individual file errors are logged and do not stop the scan.</summary>
    void DeleteExpiredLogs();
}
