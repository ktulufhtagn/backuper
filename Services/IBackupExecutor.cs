namespace Backuper.Services;

public interface IBackupExecutor
{
    Task ExecuteAsync(string scheduledDate, CancellationToken cancellationToken);
}
