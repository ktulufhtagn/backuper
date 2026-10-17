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
            var rcloneResult = await RunRcloneAsync(backupOptions, logFilePath, cancellationToken);
            var completedAt = DateTimeOffset.Now;

            execution.CompletedAtLocal = completedAt;
            execution.CompletedAtUtc = completedAt.ToUniversalTime();
            execution.ExitCode = rcloneResult.ExitCode;
            execution.Status = rcloneResult.ExitCode == 0 ? BackupStatus.Succeeded : BackupStatus.Failed;
            execution.ErrorMessage = rcloneResult.ExitCode == 0
                ? null
                : BuildFailureMessage(rcloneResult);

            state.LastBackupCompletedAtLocal = completedAt;
            state.LastBackupCompletedAtUtc = completedAt.ToUniversalTime();
            state.LastBackupStatus = execution.Status;

            if (rcloneResult.ExitCode == 0)
            {
                state.LastSuccessfulBackupAtLocal = completedAt;
                state.LastSuccessfulBackupAtUtc = completedAt.ToUniversalTime();
            }

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Backup completed with status {Status} and exit code {ExitCode}. Log: {LogFilePath}",
                execution.Status,
                rcloneResult.ExitCode,
                logFilePath);
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

    private static async Task<RcloneProcessResult> RunRcloneAsync(
        BackupOptions options,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = options.PowerShellExecutable,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add(BuildPowerShellRcloneCommand(options, logFilePath));
        process.StartInfo.Environment["PATH"] = BuildRefreshedPath();

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;
            return new RcloneProcessResult(process.ExitCode, output, error);
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

    private static string BuildPowerShellRcloneCommand(BackupOptions options, string logFilePath)
    {
        var arguments = new[]
        {
            "copy",
            options.SourcePath,
            options.Destination,
            "--progress",
            "--transfers",
            options.Transfers.ToString(),
            "--b2-upload-concurrency",
            options.B2UploadConcurrency.ToString(),
            "--bwlimit",
            options.UploadBandwidthLimit,
            "--log-file",
            logFilePath,
            "--log-level",
            "INFO"
        };

        var rcloneCommand = string.Join(" ", arguments.Select(QuotePowerShellArgument));
        var quotedLogFilePath = QuotePowerShellArgument(logFilePath);

        return string.Join(Environment.NewLine, new[]
        {
            "$ErrorActionPreference = 'Stop'",
            "try {",
            $"    $resolvedRclone = Get-Command -Name {QuotePowerShellArgument(options.RcloneExecutable)} -CommandType Application -ErrorAction Stop",
            $"    & ($resolvedRclone.Source) {rcloneCommand}",
            "    $exitCode = if ($null -eq $global:LASTEXITCODE) { if ($?) { 0 } else { 1 } } else { [int]$global:LASTEXITCODE }",
            $"    if ($exitCode -eq 0 -and -not (Test-Path -LiteralPath {quotedLogFilePath} -PathType Leaf)) {{",
            $"        [Console]::Error.WriteLine('rclone exited with code 0 but did not create the expected log file: ' + {quotedLogFilePath})",
            "        exit 1",
            "    }",
            "    exit $exitCode",
            "} catch {",
            "    [Console]::Error.WriteLine($_.Exception.Message)",
            "    [Console]::Error.WriteLine('User: ' + [System.Security.Principal.WindowsIdentity]::GetCurrent().Name)",
            "    [Console]::Error.WriteLine('RcloneExecutable: ' + " + QuotePowerShellArgument(options.RcloneExecutable) + ")",
            "    [Console]::Error.WriteLine('PATH: ' + $env:PATH)",
            "    exit 1",
            "}"
        });
    }

    private static string QuotePowerShellArgument(string value)
    {
        return $"'{value.Replace("'", "''")}'";
    }

    private static string BuildRefreshedPath()
    {
        var paths = new[]
        {
            Environment.GetEnvironmentVariable("PATH"),
            Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
            Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)
        };

        return string.Join(
            Path.PathSeparator,
            paths
                .OfType<string>()
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .SelectMany(path => path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(Environment.ExpandEnvironmentVariables)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildFailureMessage(RcloneProcessResult result)
    {
        var details = string.Join(
            Environment.NewLine,
            new[] { result.StandardError, result.StandardOutput }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()));

        if (string.IsNullOrWhiteSpace(details))
        {
            return $"rclone exited with code {result.ExitCode}.";
        }

        const int maxDetailsLength = 1800;
        if (details.Length > maxDetailsLength)
        {
            details = details[..maxDetailsLength];
        }

        return $"rclone exited with code {result.ExitCode}. {details}";
    }

    private sealed record RcloneProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
