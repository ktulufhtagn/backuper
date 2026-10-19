namespace Backuper.Models;

/// <summary>Stores the outcome and diagnostic details of one scheduled backup attempt.</summary>
public sealed class BackupExecution
{
    /// <summary>Database-generated identifier for this attempt.</summary>
    public int Id { get; set; }

    /// <summary>Local calendar date assigned to this attempt, formatted yyyy-MM-dd.</summary>
    public string ScheduledDate { get; set; } = string.Empty;

    /// <summary>Start time including the local UTC offset.</summary>
    public DateTimeOffset StartedAtLocal { get; set; }

    /// <summary>Start time normalized to UTC.</summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>Completion time with local offset, or null while running.</summary>
    public DateTimeOffset? CompletedAtLocal { get; set; }

    /// <summary>Completion time normalized to UTC, or null while running.</summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>Current persisted status from BackupStatus.</summary>
    public string Status { get; set; } = BackupStatus.InProgress;

    /// <summary>Rclone process exit code, or null when no exit code was obtained.</summary>
    public int? ExitCode { get; set; }

    /// <summary>Absolute path assigned to the rclone log for this attempt.</summary>
    public string LogFilePath { get; set; } = string.Empty;

    /// <summary>Failure or interruption detail, or null for a successful attempt.</summary>
    public string? ErrorMessage { get; set; }
}
