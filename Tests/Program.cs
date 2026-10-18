using Backuper.Desktop;
using Backuper.Models;
using Backuper.Services;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        const string sample = """{"stats":{"bytes":5242880,"totalBytes":10485760,"speed":1572864,"transfers":3,"totalTransfers":8,"checks":12,"totalChecks":14,"errors":1,"elapsedTime":12.5,"eta":4}}""";
        var parsed = RcloneStatisticsReader.Parse(sample)!;
        Assert(parsed.Bytes == 5242880 && parsed.Speed == 1572864 && parsed.EtaSeconds == 4, "parse real rclone stats fields");
        Assert(RcloneStatisticsReader.Parse("old plain text log") is null, "old text logs remain readable without fake stats");
        Assert(RcloneStatisticsReader.Parse("{\"stats\":") is null, "ignore partial JSON");
        Assert(RcloneStatisticsReader.Parse("{\"stats\":{\"eta\":null}}")?.EtaSeconds is null, "unknown ETA");
        Assert(RcloneStatisticsReader.Parse("{\"stats\":null}") is null, "invalid stats value");
        var directory = Path.Combine(Path.GetTempPath(), "BackuperTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var log = Path.Combine(directory, "rclone.log");
        var reader = new RcloneStatisticsReader();
        File.WriteAllText(log, sample[..40]);
        Assert(reader.Read(log) is null, "partial log record deferred");
        File.AppendAllText(log, sample[40..] + "\n");
        Assert(reader.Read(log)?.Bytes == 5242880, "partial record completed on next read");
        Assert(reader.Read(log)?.Bytes == 5242880, "retain latest stats between writes");
        File.WriteAllText(log, "{\"stats\":{\"bytes\":12}}\n");
        Assert(reader.Read(log)?.Bytes == 12, "log truncation resets offset");
        var next = Path.Combine(directory, "next.log");
        File.WriteAllText(next, "new backup without stats\n");
        Assert(reader.Read(next) is null, "new backup resets old stats");
        File.WriteAllText(log, new string('x', 600000) + "\n" + sample + "\n");
        Assert(reader.Read(log)?.Bytes == 5242880, "bounded tail catches up on large logs");
        Assert(BackupStatusWindow.FormatBytes(1572864) == $"{1.5:0.##} MiB", "binary transfer rate formatting");

        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        using var icon = BackuperIcon.Create();
        using var window = new BackupStatusWindow(icon);
        var snapshot = new BackupDashboard(DateTimeOffset.UtcNow, "InProgress", DateTimeOffset.Now.AddMinutes(-12), null,
            null, parsed, @"C:\Data\ToBackUp", "backup-remote:backups", "1.5Mi", "02:00", 4, 28, 1, log);
        window.UpdateStatus(snapshot, "Running", null);
        window.CreateControl();
        window.Location = new Point(-10000, -10000);
        window.Show();
        Application.DoEvents();
        window.PerformLayout();
        if (args.Length > 0)
        {
            using var bitmap = new Bitmap(window.Width, window.Height);
            window.DrawToBitmap(bitmap, new Rectangle(0, 0, window.Width, window.Height));
            bitmap.Save(Path.GetFullPath(args[0]));
        }
        window.UpdateStatus(snapshot with { UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) }, "Stopped", null);
        var labels = window.Controls[0].Controls.OfType<Label>().Select(x => x.Text).ToArray();
        Assert(labels.Contains("No active transfer"), "stopped service never reports live bandwidth");
        Assert(labels.Any(x => x.Contains("stale")), "stale snapshot is disclosed");
        window.UpdateStatus(null, "Not installed", null);
        Assert(window.Controls[0].Controls.OfType<Label>().Any(x => x.Text.Contains("Not installed")), "missing service state");
        File.Delete(log); File.Delete(next); Directory.Delete(directory);
        Console.WriteLine("PASS: stats parsing, partial writes, log rollover, bounded reads, rate units, and dashboard states.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Failed: " + message);
    }
}
