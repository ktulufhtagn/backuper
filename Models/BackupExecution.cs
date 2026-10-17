namespace Backuper.Models;

public sealed class BackupExecution
{
    public int Id { get; set; }

    public string ScheduledDate { get; set; } = string.Empty;

    public DateTimeOffset StartedAtLocal { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtLocal { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string Status { get; set; } = BackupStatus.InProgress;

    public int? ExitCode { get; set; }

    public string LogFilePath { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
