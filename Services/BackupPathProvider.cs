using Backuper.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

/// <summary>Resolves configured storage paths and creates the data and log directories.</summary>
public sealed class BackupPathProvider : IBackupPathProvider
{
    /// <summary>Resolves paths and creates the application-data and log directories.</summary>
    /// <param name="options">Configured storage roots and relative paths.</param>
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

    /// <inheritdoc/>
    public string AppDataDirectory { get; }

    /// <inheritdoc/>
    public string DatabasePath { get; }

    /// <inheritdoc/>
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
