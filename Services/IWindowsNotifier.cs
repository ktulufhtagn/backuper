namespace Backuper.Services;

/// <summary>Delivers backup lifecycle notifications without requiring the service to display desktop UI.</summary>
public interface IWindowsNotifier
{
    /// <summary>Notifies the user that a backup is starting.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task representing notification delivery or queue publication.</returns>
    Task NotifyBackupStartingAsync(CancellationToken cancellationToken);

    /// <summary>Notifies the user of a backup outcome and its log location.</summary>
    /// <param name="status">Persisted backup status.</param>
    /// <param name="logFilePath">Path to the log associated with the attempt.</param>
    /// <param name="errorMessage">Failure detail, or null if no error is available.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task representing notification delivery or queue publication.</returns>
    Task NotifyBackupFinishedAsync(
        string status,
        string logFilePath,
        string? errorMessage,
        CancellationToken cancellationToken);
}
