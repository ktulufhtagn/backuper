using System.Text;
using System.Text.Json;
using Backuper.Models;

namespace Backuper.Services;

// Incremental, bounded reads avoid loading multi-gigabyte backup logs into memory.
public sealed class RcloneStatisticsReader
{
    private string? path;
    private long offset;
    private string partial = "";
    private TransferStatistics? latest;

    public TransferStatistics? Read(string? file)
    {
        if (file != path)
        {
            path = file;
            offset = 0;
            partial = "";
            latest = null;
        }
        if (file is null || !File.Exists(file)) return latest;
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length < offset) { offset = 0; partial = ""; latest = null; }
        // Stats are written every five seconds. Catch up from the tail when first
        // opening an old log or when a large burst of file-copy messages arrives.
        if (stream.Length - offset > 512 * 1024)
        {
            offset = stream.Length - 512 * 1024;
            partial = "";
        }
        stream.Position = offset;
        var bytes = new byte[Math.Min(512 * 1024, (int)(stream.Length - offset))];
        var count = stream.Read(bytes, 0, bytes.Length);
        offset += count;
        var lines = (partial + Encoding.UTF8.GetString(bytes, 0, count)).Split('\n');
        partial = lines[^1].Length < 65536 ? lines[^1] : "";
        foreach (var line in lines[..^1])
        {
            var stats = Parse(line);
            if (stats is not null) latest = stats;
        }
        return latest;
    }

    public static TransferStatistics? Parse(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line);
            if (!json.RootElement.TryGetProperty("stats", out var stats)) return null;
            double Number(string key) => stats.TryGetProperty(key, out var value) && value.TryGetDouble(out var number)
                && double.IsFinite(number) ? Math.Max(0, number) : 0;
            return new TransferStatistics((long)Number("bytes"), (long)Number("totalBytes"), Number("speed"),
                (long)Number("transfers"), (long)Number("totalTransfers"), (long)Number("checks"),
                (long)Number("totalChecks"), (long)Number("errors"), Number("elapsedTime"),
                stats.TryGetProperty("eta", out var eta) && eta.ValueKind == JsonValueKind.Number ? Number("eta") : null);
        }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
}
