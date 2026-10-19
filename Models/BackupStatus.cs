namespace Backuper.Models;

/// <summary>Defines the status values persisted in backup history and shared with the desktop.</summary>
public static class BackupStatus
{
    /// <summary>No backup attempt has been recorded.</summary>
    public const string NeverRun = "NeverRun";
    /// <summary>A backup attempt is currently executing.</summary>
    public const string InProgress = "InProgress";
    /// <summary>The backup process completed successfully.</summary>
    public const string Succeeded = "Succeeded";
    /// <summary>The backup failed during startup or returned a nonzero exit code.</summary>
    public const string Failed = "Failed";
    /// <summary>The backup was cancelled or recovered as unfinished after a process restart.</summary>
    public const string Interrupted = "Interrupted";
}
