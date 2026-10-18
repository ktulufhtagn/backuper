namespace Backuper.Models;

public sealed record TransferStatistics(
    long Bytes, long TotalBytes, double Speed, long Transfers, long TotalTransfers,
    long Checks, long TotalChecks, long Errors, double ElapsedSeconds, double? EtaSeconds);

public sealed record BackupDashboard(
    DateTimeOffset UpdatedAt, string Status, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, string? Error, TransferStatistics? Statistics,
    string Source, string Destination, string BandwidthLimit, string DailyRunTime,
    int ParallelTransfers, int SuccessfulRuns, int FailedRuns, string? LogFile);
