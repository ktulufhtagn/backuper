namespace Backuper.Configuration;

public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    public string SourcePath { get; set; } = @"C:\Data\ToBackUp";

    public string Destination { get; set; } = "backup-remote:backups";

    public string RcloneExecutable { get; set; } = "rclone";

    public int Transfers { get; set; } = 4;

    public int B2UploadConcurrency { get; set; } = 8;

    public string UploadBandwidthLimit { get; set; } = "1Mi";

    public string DailyRunTime { get; set; } = "02:00";

    public int CheckIntervalMinutes { get; set; } = 30;

    public string AppDataDirectory { get; set; } = @"%USERPROFILE%\AppData\LocalLow\Backuper";

    public string DatabasePath { get; set; } = "backuper.db";

    public string LogDirectory { get; set; } = "Logs";

    public int LogRetentionDays { get; set; } = 7;
}
