using System.Diagnostics;
using Backuper.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

public sealed class WindowsToastNotifier(
    IOptions<NotificationOptions> options,
    ILogger<WindowsToastNotifier> logger) : IWindowsNotifier
{
    public async Task NotifyBackupStartingAsync(CancellationToken cancellationToken)
    {
        await NotifyAsync("Backuper", "Daily backup is starting.", cancellationToken);
    }

    public async Task NotifyBackupFinishedAsync(
        string status,
        string logFilePath,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var message = status switch
        {
            "Succeeded" => $"Daily backup completed successfully. Log: {logFilePath}",
            "Interrupted" => $"Daily backup was interrupted. Log: {logFilePath}",
            _ => $"Daily backup finished with status {status}. {errorMessage ?? string.Empty} Log: {logFilePath}"
        };

        await NotifyAsync("Backuper", Truncate(message, 240), cancellationToken);
    }

    private async Task NotifyAsync(string title, string message, CancellationToken cancellationToken)
    {
        var notificationOptions = options.Value;
        if (!notificationOptions.Enabled)
        {
            return;
        }

        if (notificationOptions.UseMessageFallback)
        {
            await TryShowMessageAsync(notificationOptions, title, message, cancellationToken);
        }

        if (notificationOptions.UseToast)
        {
            await TryShowToastAsync(notificationOptions.AppId, title, message, cancellationToken);
        }
    }

    private async Task TryShowMessageAsync(
        NotificationOptions notificationOptions,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await ShowMessageAsync(notificationOptions, $"{title}: {message}", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not show Windows message notification.");
        }
    }

    private async Task TryShowToastAsync(
        string appId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await ShowToastAsync(appId, title, message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not show Windows toast notification.");
        }
    }

    private static async Task ShowMessageAsync(
        NotificationOptions options,
        string message,
        CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = options.MessageExecutable,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            ArgumentList =
            {
                "*",
                $"/time:{options.MessageTimeoutSeconds}",
                message
            }
        });

        if (process is null)
        {
            return;
        }

        await process.WaitForExitAsync(cancellationToken);
    }

    private static async Task ShowToastAsync(string appId, string title, string message, CancellationToken cancellationToken)
    {
        var script = $$"""
            [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
            [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

            $template = @"
            <toast>
              <visual>
                <binding template="ToastGeneric">
                  <text>{{EscapeXml(title)}}</text>
                  <text>{{EscapeXml(message)}}</text>
                </binding>
              </visual>
            </toast>
            "@

            $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
            $xml.LoadXml($template)
            $toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
            $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier("{{EscapePowerShell(appId)}}")
            $notifier.Show($toast)
            """;

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            ArgumentList =
            {
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-Command",
                script
            }
        });

        if (process is null)
        {
            return;
        }

        await process.WaitForExitAsync(cancellationToken);
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string EscapePowerShell(string value)
    {
        return value.Replace("\"", "`\"");
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
