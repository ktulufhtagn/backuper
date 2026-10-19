namespace Backuper.Services;

/// <summary>Determines whether a daily backup is due and runs eligible attempts.</summary>
public interface IBackupScheduler
{
    /// <summary>Applies retention and runs a backup if the schedule and recorded state permit it.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes after the check and any selected backup attempt.</returns>
    Task RunIfDueAsync(CancellationToken cancellationToken);
}
