namespace Backuper.Models;

/// <summary>Stores the singleton summary of the latest backup activity and success.</summary>
public sealed class BackupState
{
    /// <summary>Identifier reserved for the single aggregate state row.</summary>
    public const int SingletonId = 1;

    /// <summary>Primary key of the singleton state row.</summary>
    public int Id { get; set; }

    /// <summary>Most recent start time with local offset, or null before any attempt.</summary>
    public DateTimeOffset? LastBackupStartedAtLocal { get; set; }

    /// <summary>Most recent start time in UTC, or null before any attempt.</summary>
    public DateTimeOffset? LastBackupStartedAtUtc { get; set; }

    /// <summary>Most recent completion time with local offset, or null when none is recorded.</summary>
    public DateTimeOffset? LastBackupCompletedAtLocal { get; set; }

    /// <summary>Most recent completion time in UTC, or null when none is recorded.</summary>
    public DateTimeOffset? LastBackupCompletedAtUtc { get; set; }

    /// <summary>Most recent successful completion with local offset, or null when no backup has succeeded.</summary>
    public DateTimeOffset? LastSuccessfulBackupAtLocal { get; set; }

    /// <summary>Most recent successful completion in UTC, or null when no backup has succeeded.</summary>
    public DateTimeOffset? LastSuccessfulBackupAtUtc { get; set; }

    /// <summary>Status of the most recent attempt, or NeverRun before the first attempt.</summary>
    public string LastBackupStatus { get; set; } = BackupStatus.NeverRun;
}
