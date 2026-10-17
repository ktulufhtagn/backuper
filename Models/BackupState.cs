namespace Backuper.Models;

public sealed class BackupState
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public DateTimeOffset? LastBackupStartedAtLocal { get; set; }

    public DateTimeOffset? LastBackupStartedAtUtc { get; set; }

    public DateTimeOffset? LastBackupCompletedAtLocal { get; set; }

    public DateTimeOffset? LastBackupCompletedAtUtc { get; set; }

    public DateTimeOffset? LastSuccessfulBackupAtLocal { get; set; }

    public DateTimeOffset? LastSuccessfulBackupAtUtc { get; set; }

    public string LastBackupStatus { get; set; } = BackupStatus.NeverRun;
}
