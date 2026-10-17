using Backuper.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

public sealed class BackupPathProvider : IBackupPathProvider
{
    public BackupPathProvider(IOptions<BackupOptions> options)
    {
        var backupOptions = options.Value;

        AppDataDirectory = ResolveFullPath(backupOptions.AppDataDirectory);
        DatabasePath = ResolveUnderAppData(backupOptions.DatabasePath, AppDataDirectory);
        LogDirectory = ResolveUnderAppData(backupOptions.LogDirectory, AppDataDirectory);

        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        Directory.CreateDirectory(LogDirectory);
    }

    public string AppDataDirectory { get; }

    public string DatabasePath { get; }

    public string LogDirectory { get; }

    private static string ResolveUnderAppData(string configuredPath, string appDataDirectory)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
        return Path.GetFullPath(Path.IsPathRooted(expandedPath)
            ? expandedPath
            : Path.Combine(appDataDirectory, expandedPath));
    }

    private static string ResolveFullPath(string configuredPath)
    {
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
    }
}
