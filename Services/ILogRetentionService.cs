namespace Backuper.Services;

public interface ILogRetentionService
{
    void DeleteExpiredLogs();
}
