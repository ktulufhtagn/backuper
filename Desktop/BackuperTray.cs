using System.ComponentModel;
using System.Text.Json;
using Backuper.Models;
using Backuper.Services;

namespace Backuper.Desktop;

/// <summary>Owns the notification-area icon, status window, polling timer, and stop-and-exit menu.</summary>
public sealed class BackuperTray : ApplicationContext
{
    private readonly Icon icon = BackuperIcon.Create();
    private readonly NotifyIcon tray;
    private readonly BackupStatusWindow window;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 2000 };
    private readonly string statusPath;
    private bool refreshing;
    private bool exiting;

    /// <summary>Creates the tray icon, status window, and dashboard polling loop.</summary>
    /// <param name="queue">Shared queue used to locate the service dashboard snapshot.</param>
    public BackuperTray(NotificationQueue queue)
    {
        statusPath = Path.Combine(queue.DirectoryPath, "Dashboard", "status.json");
        window = new BackupStatusWindow(icon);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Backuper", null, (_, _) => OpenWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit (stop service)", null, async (_, _) => await StopAndExitAsync());
        tray = new NotifyIcon { Icon = icon, Text = "Backuper", ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) OpenWindow(); };
        timer.Tick += async (_, _) => await RefreshAsync();
        timer.Start();
        _ = RefreshAsync();
    }

    private void OpenWindow()
    {
        if (!exiting) timer.Start();
        window.ShowAtTray();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (refreshing || exiting) return;
        refreshing = true;
        try
        {
            var service = await Task.Run(BackuperServiceControl.Status);
            BackupDashboard? snapshot = null;
            string? error = null;
            try
            {
                if (File.Exists(statusPath) && new FileInfo(statusPath).Length <= 128 * 1024)
                    snapshot = JsonSerializer.Deserialize<BackupDashboard>(await File.ReadAllTextAsync(statusPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            { error = "Cannot read service status. Check the shared status directory permissions."; }
            if (exiting || window.IsDisposed) return;
            window.UpdateStatus(snapshot, service, error);
            tray.Text = $"Backuper · {service}";
        }
        finally { refreshing = false; }
    }

    private async Task StopAndExitAsync()
    {
        if (exiting) return;
        exiting = true;
        window.ShowAtTray();
        window.SetNotice("Stopping the backup service… Windows may ask for administrator permission.");
        try
        {
            await BackuperServiceControl.StopWithElevationAsync();
            tray.Visible = false;
            ExitThread();
        }
        catch (Exception ex)
        {
            exiting = false;
            // Keep the error visible until the user reopens the window or retries.
            timer.Stop();
            window.SetNotice(ex is Win32Exception { NativeErrorCode: 1223 }
                ? "Permission request cancelled. Backuper is still running."
                : $"Could not stop the service: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose(); tray.Visible = false; tray.ContextMenuStrip?.Dispose(); tray.Dispose(); window.Dispose(); icon.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Runs the desktop message loop on a dedicated STA thread.</summary>
    /// <remarks>Does not start or stop the backup host by itself; service stopping is an explicit menu action.</remarks>
    /// <param name="queue">Shared queue used by the desktop companion.</param>
    /// <returns>A task that completes when the tray message loop exits.</returns>
    public static Task RunAsync(NotificationQueue queue)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                using var context = new BackuperTray(queue);
                Application.Run(context);
                completion.SetResult();
            }
            catch (Exception exception) { completion.SetException(exception); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
