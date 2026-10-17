namespace Backuper.Services;

public interface IBackupScheduler
{
    Task RunIfDueAsync(CancellationToken cancellationToken);
}
