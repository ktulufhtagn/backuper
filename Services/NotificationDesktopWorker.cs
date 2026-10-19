using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Backuper.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

/// <summary>Displays queued notifications and stores per-user receipts to avoid replay on normal restarts.</summary>
public sealed class NotificationDesktopWorker(
    NotificationQueue queue,
    IOptions<NotificationOptions> options,
    ILogger<NotificationDesktopWorker> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiptFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Backuper", "NotificationReceipts",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(queue.DirectoryPath.ToUpperInvariant()))) + ".json");
        var seen = await ReadReceiptsAsync(receiptFile, stoppingToken);
        logger.LogInformation("Desktop notification companion listening at {Path}", queue.DirectoryPath);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.Enabled && options.Value.UseToast && Directory.Exists(queue.DirectoryPath))
                {
                    var files = Directory.GetFiles(queue.DirectoryPath, "*.json");
                    seen.IntersectWith(files.Select(Path.GetFileName).OfType<string>());
                    foreach (var file in files.OrderBy(File.GetLastWriteTimeUtc))
                    {
                        var id = Path.GetFileName(file);
                        if (seen.Contains(id)) continue;
                        try
                        {
                            if (new FileInfo(file).Length > 16384)
                                throw new InvalidDataException("Notification exceeds the allowed size.");
                            var notification = JsonSerializer.Deserialize<NotificationQueue.Notification>(
                                await File.ReadAllTextAsync(file, stoppingToken));
                            if (notification is not null &&
                                !string.IsNullOrWhiteSpace(notification.Message))
                            {
                                var message = notification.Message;
                                if (DateTimeOffset.UtcNow - notification.CreatedAt > TimeSpan.FromMinutes(1))
                                    message = $"{notification.CreatedAt.ToLocalTime():g}: {message}";
                                WindowsToastNotifier.ShowToast(message);
                                logger.LogInformation("Displayed queued notification {Id}", id);
                            }
                            seen.Add(id);
                            await SaveReceiptsAsync(receiptFile, seen, stoppingToken);
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            logger.LogWarning(exception, "Could not display queued notification {Path}", file);
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not read the desktop notification queue.");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task<HashSet<string>> ReadReceiptsAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(path))
                return new HashSet<string>(
                    JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(path, cancellationToken)) ?? [],
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read notification receipts; queued notifications may be repeated.");
        }
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SaveReceiptsAsync(string path, HashSet<string> receipts, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(receipts), cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }
}
