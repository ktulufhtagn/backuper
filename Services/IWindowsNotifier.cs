namespace Backuper.Services;

public interface IWindowsNotifier
{
    Task NotifyBackupStartingAsync(CancellationToken cancellationToken);
}
