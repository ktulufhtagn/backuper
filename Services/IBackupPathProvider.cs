namespace Backuper.Services;

public interface IBackupPathProvider
{
    string AppDataDirectory { get; }

    string DatabasePath { get; }

    string LogDirectory { get; }
}
