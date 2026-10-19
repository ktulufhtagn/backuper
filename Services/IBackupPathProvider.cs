namespace Backuper.Services;

/// <summary>Provides resolved paths for backup data, history, and logs.</summary>
public interface IBackupPathProvider
{
    /// <summary>Absolute root directory for backup application data.</summary>
    string AppDataDirectory { get; }

    /// <summary>Absolute path to the SQLite backup history database.</summary>
    string DatabasePath { get; }

    /// <summary>Absolute directory containing rclone logs.</summary>
    string LogDirectory { get; }
}
