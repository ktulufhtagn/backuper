namespace Backuper.Configuration;

/// <summary>Configures the backup schedule, rclone process, and local persistence paths.</summary>
public sealed class BackupOptions
{
    /// <summary>Configuration section containing backup settings.</summary>
    public const string SectionName = "Backup";

    /// <summary>Source directory to copy; must be explicitly configured before the backup host starts.</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>Destination accepted by rclone, typically a configured remote and path; required before startup.</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Executable name resolved through PATH, or an absolute path to rclone.</summary>
    public string RcloneExecutable { get; set; } = "rclone";

    // Use an absolute path for service deployments, independent of the account profile.
    /// <summary>Optional rclone config file; environment variables expand for the process account and relative paths use the application directory. Null uses rclone defaults.</summary>
    public string? RcloneConfigPath { get; set; }

    /// <summary>PowerShell executable used to launch rclone.</summary>
    public string PowerShellExecutable { get; set; } = "powershell.exe";

    /// <summary>Maximum number of file transfers rclone performs concurrently.</summary>
    public int Transfers { get; set; } = 4;

    /// <summary>Per-file upload concurrency passed to the Backblaze B2 backend.</summary>
    public int B2UploadConcurrency { get; set; } = 8;

    /// <summary>Rclone bandwidth limit expression, such as 1.5Mi; passed unchanged to --bwlimit.</summary>
    public string UploadBandwidthLimit { get; set; } = "1Mi";

    /// <summary>Earliest local time for the daily backup, in HH:mm format.</summary>
    public string DailyRunTime { get; set; } = "02:00";

    /// <summary>Delay in minutes between scheduler checks.</summary>
    public int CheckIntervalMinutes { get; set; } = 30;

    /// <summary>Root storage directory; environment variables expand under the account running the process.</summary>
    public string AppDataDirectory { get; set; } = @"%USERPROFILE%\AppData\LocalLow\Backuper";

    /// <summary>SQLite database path, relative to AppDataDirectory unless absolute.</summary>
    public string DatabasePath { get; set; } = "backuper.db";

    /// <summary>Log directory path, relative to AppDataDirectory unless absolute.</summary>
    public string LogDirectory { get; set; } = "Logs";

    /// <summary>Age in days after which log files are eligible for deletion, based on last-write time.</summary>
    public int LogRetentionDays { get; set; } = 7;
}
