using System.Diagnostics;
using Backuper.Configuration;
using Backuper.Data;
using Backuper.Models;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

public sealed class RcloneBackupExecutor(
    BackupDbContext database,
    IWindowsNotifier notifier,
    IBackupPathProvider pathProvider,
    IOptions<BackupOptions> options,
    ILogger<RcloneBackupExecutor> logger) : IBackupExecutor
{
    public async Task ExecuteAsync(string scheduledDate, CancellationToken cancellationToken)
    {
        var backupOptions = options.Value;
        var startedAt = DateTimeOffset.Now;
        var logFilePath = BuildLogFilePath(pathProvider.LogDirectory, startedAt);

        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        var execution = new BackupExecution
        {
            ScheduledDate = scheduledDate,
            StartedAtLocal = startedAt,
            StartedAtUtc = startedAt.ToUniversalTime(),
            Status = BackupStatus.InProgress,
            LogFilePath = logFilePath
        };

        database.BackupExecutions.Add(execution);

        var state = await database.GetOrCreateStateAsync(cancellationToken);
        state.LastBackupStartedAtLocal = startedAt;
        state.LastBackupStartedAtUtc = startedAt.ToUniversalTime();
        state.LastBackupStatus = BackupStatus.InProgress;
        await database.SaveChangesAsync(cancellationToken);

        await notifier.NotifyBackupStartingAsync(cancellationToken);

        try
        {
            var exitCode = await RunRcloneAsync(backupOptions, logFilePath, cancellationToken);
            var completedAt = DateTimeOffset.Now;

            execution.CompletedAtLocal = completedAt;
            execution.CompletedAtUtc = completedAt.ToUniversalTime();
            execution.ExitCode = exitCode;
            execution.Status = exitCode == 0 ? BackupStatus.Succeeded : BackupStatus.Failed;
            execution.ErrorMessage = exitCode == 0 ? null : $"rclone exited with code {exitCode}.";

            state.LastBackupCompletedAtLocal = completedAt;
            state.LastBackupCompletedAtUtc = completedAt.ToUniversalTime();
            state.LastBackupStatus = execution.Status;

            if (exitCode == 0)
            {
                state.LastSuccessfulBackupAtLocal = completedAt;
                state.LastSuccessfulBackupAtUtc = completedAt.ToUniversalTime();
            }

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Backup completed with status {Status}. Log: {LogFilePath}", execution.Status, logFilePath);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var completedAt = DateTimeOffset.Now;

            execution.CompletedAtLocal = completedAt;
            execution.CompletedAtUtc = completedAt.ToUniversalTime();
            execution.Status = BackupStatus.Failed;
            execution.ErrorMessage = exception.Message;

            state.LastBackupCompletedAtLocal = completedAt;
            state.LastBackupCompletedAtUtc = completedAt.ToUniversalTime();
            state.LastBackupStatus = BackupStatus.Failed;

            await database.SaveChangesAsync(CancellationToken.None);
            logger.LogError(exception, "Backup failed before rclone completed. Log: {LogFilePath}", logFilePath);
        }
    }

    private static async Task<int> RunRcloneAsync(
        BackupOptions options,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = options.RcloneExecutable,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        process.StartInfo.ArgumentList.Add("copy");
        process.StartInfo.ArgumentList.Add(options.SourcePath);
        process.StartInfo.ArgumentList.Add(options.Destination);
        process.StartInfo.ArgumentList.Add("--progress");
        process.StartInfo.ArgumentList.Add("--transfers");
        process.StartInfo.ArgumentList.Add(options.Transfers.ToString());
        process.StartInfo.ArgumentList.Add("--b2-upload-concurrency");
        process.StartInfo.ArgumentList.Add(options.B2UploadConcurrency.ToString());
        process.StartInfo.ArgumentList.Add("--bwlimit");
        process.StartInfo.ArgumentList.Add(options.UploadBandwidthLimit);
        process.StartInfo.ArgumentList.Add("--log-file");
        process.StartInfo.ArgumentList.Add(logFilePath);
        process.StartInfo.ArgumentList.Add("--log-level");
        process.StartInfo.ArgumentList.Add("INFO");

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(outputTask, errorTask);
            return process.ExitCode;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }
    }

    private static string BuildLogFilePath(string logDirectory, DateTimeOffset startedAt)
    {
        var fileName = $"backup_{startedAt:yyyyMMdd_HHmmss}.log";
        return Path.GetFullPath(Path.Combine(logDirectory, fileName));
    }
}
