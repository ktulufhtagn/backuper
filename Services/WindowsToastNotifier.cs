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
        var notificationOptions = options.Value;
        if (!notificationOptions.Enabled)
        {
            return;
        }

        try
        {
            await ShowToastAsync(notificationOptions.AppId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not show Windows backup notification.");
        }
    }

    private static async Task ShowToastAsync(string appId, CancellationToken cancellationToken)
    {
        const string title = "Backuper";
        const string message = "Daily backup is starting.";

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
}
