using System.Diagnostics;
using Backuper.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Backuper.Services;

/// <summary>Shows native toasts in a desktop session or queues them when running as a service.</summary>
public sealed class WindowsToastNotifier(
    IOptions<NotificationOptions> options,
    NotificationQueue queue,
    ILogger<WindowsToastNotifier> logger) : IWindowsNotifier
{
    /// <inheritdoc/>
    public Task NotifyBackupStartingAsync(CancellationToken cancellationToken) =>
        NotifyAsync("Daily backup is starting.", cancellationToken);

    /// <inheritdoc/>
    public Task NotifyBackupFinishedAsync(
        string status, string logFilePath, string? errorMessage, CancellationToken cancellationToken)
    {
        var message = status switch
        {
            "Succeeded" => $"Daily backup completed successfully. Log: {logFilePath}",
            "Interrupted" => $"Daily backup was interrupted. Log: {logFilePath}",
            _ => $"Daily backup finished with status {status}. {errorMessage ?? string.Empty} Log: {logFilePath}"
        };
        return NotifyAsync(message.Length <= 240 ? message : message[..240], cancellationToken);
    }

    private async Task NotifyAsync(string message, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || !options.Value.UseToast)
            return;

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var process = Process.GetCurrentProcess();
            if (process.SessionId == 0)
            {
                await queue.EnqueueAsync(message, cancellationToken);
            }
            else
            {
                ShowToast(message);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not deliver Windows notification.");
        }
    }

    /// <summary>Shows a native Windows toast from the current desktop session.</summary>
    /// <remarks>Requires a signed-in desktop session. Notification-registration failures propagate to the caller.</remarks>
    /// <param name="message">Plain message text displayed below the Backuper title.</param>
    public static void ShowToast(string message)
    {
        new ToastContentBuilder().AddText("Backuper").AddText(message).Show();
    }
}
