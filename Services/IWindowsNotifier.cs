namespace Backuper.Services;

public interface IWindowsNotifier
{
    Task NotifyBackupStartingAsync(CancellationToken cancellationToken);

    Task NotifyBackupFinishedAsync(
        string status,
        string logFilePath,
        string? errorMessage,
        CancellationToken cancellationToken);
}
