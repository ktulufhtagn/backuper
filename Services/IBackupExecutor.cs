namespace Backuper.Services;

/// <summary>Executes and records one scheduled backup attempt.</summary>
public interface IBackupExecutor
{
    /// <summary>Runs a backup and records success, failure, or interruption.</summary>
    /// <param name="scheduledDate">Local date assigned to the attempt, formatted yyyy-MM-dd.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes after the attempt and completion notification have been processed.</returns>
    Task ExecuteAsync(string scheduledDate, CancellationToken cancellationToken);
}
