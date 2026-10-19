using Backuper.Models;

namespace Backuper.Desktop;

/// <summary>Displays read-only backup settings and progress without owning the backup process.</summary>
public sealed class BackupStatusWindow : Form
{
    private static readonly Color Muted = Color.FromArgb(155, 168, 192);
    private static readonly Color Accent = Color.FromArgb(94, 234, 212);
    private readonly Label state = new();
    private readonly Label detail = new();
    private readonly Label transferred = new();
    private readonly Label speed = new();
    private readonly Label timing = new();
    private readonly Label files = new();
    private readonly Label history = new();
    private readonly Label config = new();
    private readonly Label notice = new();
    private readonly Panel progress = new();
    private double fraction;

    /// <summary>Builds the read-only status window.</summary>
    /// <param name="icon">Application icon owned by the tray context.</param>
    public BackupStatusWindow(Icon icon)
    {
        Text = "Backuper";
        Icon = icon;
        ClientSize = new Size(460, 605);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(15, 21, 33);
        ForeColor = Color.FromArgb(235, 241, 250);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 12 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        var title = new Label { Text = "BACKUPER", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Accent };
        Add(layout, title, 42);
        state.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        Add(layout, state, 34);
        detail.ForeColor = Muted;
        Add(layout, detail, 30);
        progress.Dock = DockStyle.Fill;
        progress.Margin = new Padding(0, 8, 0, 12);
        progress.Paint += (_, e) =>
        {
            e.Graphics.Clear(Color.FromArgb(38, 49, 69));
            using var brush = new SolidBrush(Accent);
            e.Graphics.FillRectangle(brush, 0, 0, (int)(progress.Width * fraction), progress.Height);
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.Controls.Add(progress, 0, 3);
        transferred.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        Add(layout, transferred, 36);
        speed.ForeColor = Accent;
        Add(layout, speed, 36);
        Add(layout, timing, 36);
        Add(layout, files, 36);
        history.ForeColor = Muted;
        Add(layout, history, 34);
        config.ForeColor = Muted;
        config.AutoEllipsis = true;
        Add(layout, config, 156);
        notice.ForeColor = Color.FromArgb(255, 192, 120);
        Add(layout, notice, 70);
        UpdateStatus(null, "Checking", null);
        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
        };
    }

    private static void Add(TableLayoutPanel layout, Label label, int height)
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = Padding.Empty;
        var row = layout.Controls.Count;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.Controls.Add(label, 0, row);
    }

    /// <summary>Shows and activates the window near the taskbar on the screen containing the pointer.</summary>
    public void ShowAtTray()
    {
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Max(area.Left, area.Right - Width - 12), Math.Max(area.Top, area.Bottom - Height - 12));
        Show();
        Activate();
    }

    /// <summary>Replaces the explanatory or error message in the window.</summary>
    /// <param name="text">Message to display.</param>
    public void SetNotice(string text) => notice.Text = text;

    /// <summary>Refreshes the display and suppresses live speed when service data is stale or stopped.</summary>
    /// <param name="data">Latest service snapshot, or null before one is available.</param>
    /// <param name="service">Windows service status or a service-query fallback label.</param>
    /// <param name="error">Optional read error that takes precedence over the snapshot notice.</param>
    public void UpdateStatus(BackupDashboard? data, string service, string? error)
    {
        var stale = data is not null && DateTimeOffset.UtcNow - data.UpdatedAt > TimeSpan.FromSeconds(15);
        var running = data?.Status == "InProgress" && !stale && service == "Running";
        state.Text = service != "Running" ? $"Service · {service}" : stale ? "Waiting for service updates" : data?.Status switch
        {
            "InProgress" => "Backup in progress", "Succeeded" => "Backup completed", "Failed" => "Backup failed",
            "Interrupted" => "Backup interrupted", _ => "Ready for next backup"
        };
        detail.Text = data is null ? "Waiting for the service to publish its status" :
            $"{(stale ? "Last update" : "Updated")} {data.UpdatedAt.ToLocalTime():g}";
        var stats = data?.Statistics;
        fraction = stats is { TotalBytes: > 0 } ? Math.Clamp((double)stats.Bytes / stats.TotalBytes, 0, 1) : 0;
        progress.Invalidate();
        transferred.Text = stats is null ? "Transfer statistics not available yet" :
            $"{FormatBytes(stats.Bytes)} / {FormatBytes(stats.TotalBytes)}  ·  {(stats.TotalBytes > 0 ? $"{fraction:P0}" : "Scanning")}";
        speed.Text = running && stats is not null ? $"{FormatBytes(stats.Speed)}/s  ·  {stats.Speed * 8 / 1_000_000:F2} Mbps" : "No active transfer";
        timing.Text = stats is null ? "Elapsed —     Remaining —" :
            $"Elapsed {Duration(stats.ElapsedSeconds)}     Remaining {(running && stats.EtaSeconds is double eta ? Duration(eta) : "—")}";
        files.Text = stats is null ? "Files —     Checks —     Errors —" :
            $"Files {stats.Transfers:N0}/{stats.TotalTransfers:N0}   Checks {stats.Checks:N0}/{stats.TotalChecks:N0}   Errors {stats.Errors:N0}";
        history.Text = data is null ? "Backup history unavailable" : $"History · {data.SuccessfulRuns} successful / {data.FailedRuns} failed";
        config.Text = data is null ? "Settings will appear when the service connects." :
            $"DAILY AT {data.DailyRunTime}  ·  LIMIT {data.BandwidthLimit}/s  ·  {data.ParallelTransfers} TRANSFERS\n\nSOURCE\n{data.Source}\n\nDESTINATION\n{data.Destination}";
        notice.Text = error ?? (stale ? "Service data is stale; figures above are from the last update." : data?.Error ??
            "Close this window to keep Backuper in the tray. Right-click the tray icon to stop the service and exit.");
    }

    /// <summary>Formats a byte quantity using binary units and the current culture.</summary>
    /// <param name="bytes">Nonnegative byte quantity.</param>
    /// <returns>A display value in B, KiB, MiB, GiB, or TiB.</returns>
    public static string FormatBytes(double bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        var index = 0;
        while (bytes >= 1024 && index < units.Length - 1) { bytes /= 1024; index++; }
        return $"{bytes:0.##} {units[index]}";
    }

    private static string Duration(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, 315360000));
        return time.TotalDays >= 1 ? time.ToString(@"d\d\ hh\:mm\:ss") : time.ToString(@"hh\:mm\:ss");
    }
}
