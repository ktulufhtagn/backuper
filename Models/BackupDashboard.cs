namespace Backuper.Models;

/// <summary>Describes the latest counters reported by rclone; totals may change during file discovery.</summary>
/// <param name="Bytes">Bytes transferred so far.</param>
/// <param name="TotalBytes">Currently discovered total bytes to transfer.</param>
/// <param name="Speed">Rclone-reported transfer rate in bytes per second, not total adapter traffic.</param>
/// <param name="Transfers">Completed file transfers.</param>
/// <param name="TotalTransfers">Currently discovered total file transfers.</param>
/// <param name="Checks">Completed file checks.</param>
/// <param name="TotalChecks">Currently discovered total file checks.</param>
/// <param name="Errors">Errors reported by rclone for the attempt.</param>
/// <param name="ElapsedSeconds">Elapsed transfer time in seconds.</param>
/// <param name="EtaSeconds">Estimated seconds remaining, or null when unknown.</param>
public sealed record TransferStatistics(
    long Bytes, long TotalBytes, double Speed, long Transfers, long TotalTransfers,
    long Checks, long TotalChecks, long Errors, double ElapsedSeconds, double? EtaSeconds);

/// <summary>Read-only snapshot shared by the service with the tray dashboard.</summary>
/// <param name="UpdatedAt">UTC time at which the snapshot was published.</param>
/// <param name="Status">Latest backup state, or Idle if no attempt exists.</param>
/// <param name="StartedAt">Latest attempt start time, or null before the first attempt.</param>
/// <param name="CompletedAt">Latest completion time, or null while running.</param>
/// <param name="Error">Failure or interruption detail, if available.</param>
/// <param name="Statistics">Latest structured log statistics, or null when unavailable.</param>
/// <param name="Source">Configured source directory.</param>
/// <param name="Destination">Configured rclone destination.</param>
/// <param name="BandwidthLimit">Configured rclone bandwidth-limit expression.</param>
/// <param name="DailyRunTime">Configured daily start time in local HH:mm format.</param>
/// <param name="ParallelTransfers">Configured maximum parallel file transfers.</param>
/// <param name="SuccessfulRuns">Number of successful attempts in recorded history.</param>
/// <param name="FailedRuns">Number of failed attempts in recorded history, excluding interruptions.</param>
/// <param name="LogFile">Latest attempt log path, or null before the first attempt.</param>
public sealed record BackupDashboard(
    DateTimeOffset UpdatedAt, string Status, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, string? Error, TransferStatistics? Statistics,
    string Source, string Destination, string BandwidthLimit, string DailyRunTime,
    int ParallelTransfers, int SuccessfulRuns, int FailedRuns, string? LogFile);
