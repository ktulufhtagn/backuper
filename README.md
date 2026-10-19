# Backuper

Small Windows background service that runs a daily `rclone` backup to a remote you configure.

## What it does

- Starts automatically as a Windows Service.
- Checks every 30 minutes whether today's backup has already succeeded.
- Runs the backup after `02:00` local time if no successful backup exists for the current day.
- Runs as soon as the PC is back on if it missed the scheduled time while powered off.
- Stores backup history and last status in SQLite.
- Writes rclone logs and removes logs older than 7 days.
- Sends a Windows notification when a backup starts and when it ends.

## Prerequisites

- Windows 10 version 1809 or later.
- .NET 10 **Windows Desktop Runtime** installed (the tray dashboard uses Windows Forms).
- `rclone` installed and available to the service, either:
  - in the system `PATH`, or
  - configured with an absolute path in `Backup:RcloneExecutable`.
- PowerShell available to the service, configured through `Backup:PowerShellExecutable`.
- An rclone remote configured for the service account.

Verify rclone manually before installing the service:

```powershell
rclone lsd backup-remote:backups
```

If the service runs as a Windows Service, verify using the same Windows account configured for the service. A common failure mode is that `rclone` works in your interactive terminal but is not available in the service process `PATH`. The app refreshes `PATH` from the Windows user and machine environment before launching PowerShell; if it still cannot resolve `rclone`, set `Backup:RcloneExecutable` to the full path of `rclone.exe`.

## Configuration

The repository contains generic defaults and examples only. Source and destination are empty by default, and the backup host refuses to start until both are configured.

Copy `appsettings.Production.example.json` to `appsettings.Production.json` and replace its example paths and remote with your own. For IDE runs, put overrides in `appsettings.Development.local.json`. Both files are ignored by Git. The `backup-remote:backups` destination shown below is an example, not a preconfigured account.

Settings load in this order: `appsettings.json`, `appsettings.{Environment}.json`, `appsettings.Local.json`, `appsettings.{Environment}.local.json`, environment variables, then command-line arguments. Later values override earlier ones. Local overrides are available in local builds but excluded from published output. The private production configuration is also excluded from published output; copy it into the installed directory yourself. Configure rclone credentials separately using `rclone config`; never add `rclone.conf`, tokens, or logs to the repository.

Example backup settings (edit before use):

```json
{
  "Backup": {
    "SourcePath": "C:\\Data\\ToBackUp",
    "Destination": "backup-remote:backups",
    "RcloneExecutable": "rclone",
    "RcloneConfigPath": null,
    "PowerShellExecutable": "powershell.exe",
    "Transfers": 4,
    "B2UploadConcurrency": 8,
    "UploadBandwidthLimit": "1Mi",
    "DailyRunTime": "02:00",
    "CheckIntervalMinutes": 30,
    "AppDataDirectory": "%USERPROFILE%\\AppData\\LocalLow\\Backuper",
    "DatabasePath": "backuper.db",
    "LogDirectory": "Logs",
    "LogRetentionDays": 7
  },
  "Notifications": {
    "Enabled": true,
    "AppId": "Backuper",
    "UseToast": true,
    "QueueDirectory": "%ProgramData%\\Backuper\\Notifications"
  }
}
```

Production data is stored under:

```text
%USERPROFILE%\AppData\LocalLow\Backuper
```

That directory is created automatically and contains:

- `backuper.db`: SQLite database with backup executions and last status.
- `Logs\`: rclone backup logs.

## Backup command

The service launches PowerShell and runs the equivalent of:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& 'rclone' 'copy' 'C:\Data\ToBackUp' 'backup-remote:backups' '--progress' '--transfers' '4' '--b2-upload-concurrency' '8' '--bwlimit' '1Mi' '--log-file' '<log-file>' '--log-level' 'INFO'; exit $LASTEXITCODE"
```

`--bwlimit 1Mi` limits upload bandwidth to 1 MiB/s so the internet connection remains usable during backup.

The service treats the backup as failed if PowerShell exits with a non-zero code or if `rclone` does not create the expected log file.

### Works in the IDE but fails as a service

The IDE normally runs as your Windows user; the default Windows Service account is LocalSystem. Its PATH and rclone configuration directory differ. Set both `Backup:RcloneExecutable` and `Backup:RcloneConfigPath` to absolute paths in the **installed** `appsettings.Production.json`:

```json
"RcloneExecutable": "C:\\Tools\\rclone\\rclone.exe",
"RcloneConfigPath": "C:\\Users\\YOUR_USER\\AppData\\Roaming\\rclone\\rclone.conf"
```

The service account must be able to execute rclone and read that configuration file and the source directory. `RcloneConfigPath` is passed as rclone's `--config` argument. A null value preserves rclone's default lookup for the current account. Relative config paths resolve against the application directory; environment variables expand for the service account, so `%APPDATA%` still points to LocalSystem's profile when used by LocalSystem. See [rclone's Windows service guidance](https://rclone.org/install/).

Verify the file contains the expected remote without running a backup:

```powershell
& 'C:\Tools\rclone\rclone.exe' listremotes --config 'C:\Users\YOUR_USER\AppData\Roaming\rclone\rclone.conf' --ask-password=false
```

Republish the application after code changes, deploy the updated production configuration, and restart the service. Updating source settings alone does not update an already published installation. Nonzero rclone exits are logged as errors, including captured output, so they can be inspected in the Windows Application event log. The resolved account and config path are also logged at Information level; config contents are never logged by Backuper.

## Shutdown Behavior

If the console app is closed with `Ctrl+C` or the Windows Service is stopped while a backup is running:

- the service cancellation token is triggered;
- the running PowerShell/rclone process tree is killed;
- the current execution is saved as `Interrupted` in SQLite;
- an end notification is sent with the interrupted status.

If the process is force-killed, crashes, or Windows terminates it without graceful shutdown, any `InProgress` backup is marked as `Interrupted` on the next startup.

## Notifications

The app sends notifications at backup start and end.

Notifications use native Windows toast banners and Notification Center, registered as **Backuper**. No message boxes are shown. Windows notification settings and Do Not Disturb control whether banners appear. Clicking a notification does not open the log.

When running interactively, the app sends the toast directly through Microsoft's .NET notification library. Notification delivery does not launch PowerShell or register scheduled tasks.

A Windows Service cannot display UI on your desktop. It writes notification text to `Notifications:QueueDirectory` (default `%ProgramData%\Backuper\Notifications`). A desktop companion reads messages and shows the banners. Enable it once, as your normal signed-in user, from the permanent published installation directory:

```powershell
.\publish\Backuper.exe --install-notifications
```

This creates a **Backuper Notifications** shortcut in your Windows Startup folder and starts the companion immediately. It then starts automatically at every sign-in, without a console window or any daily action. The backup service still starts at boot. If the backup starts before you sign in, the companion delivers its queued notifications at sign-in. The companion does not run backups. Delete this shortcut from `shell:startup` to disable automatic startup; rerun setup if you move the installation directory. The `--notifications` switch is the internal companion mode used by the shortcut.

Unread queued messages are delivered even when the backup started or finished before login. Messages delayed by more than a minute include the event's local date and time. Per-user delivery receipts in `%LOCALAPPDATA%\Backuper\NotificationReceipts` prevent repeats across normal restarts. A crash between showing a toast and saving its receipt can repeat that notification. Multiple users running a companion will each receive notifications. Messages older than seven days are removed by the service.

The service account needs write access to the queue directory, and the desktop account needs read access. The default ProgramData permissions normally provide this when the service creates the directory. For custom permissions, grant read access only to intended recipients; messages can contain log paths and error details. Use matching `Notifications:AppId` and `QueueDirectory` values in the service and companion. `AppId` separates queues; Microsoft’s library manages the Windows app identity. The service removes messages older than seven days.

Test a native notification without starting a backup:

```powershell
.\publish\Backuper.exe --test-notification
```

Set `Notifications:Enabled` or `Notifications:UseToast` to `false` to disable backup notifications. The explicit test command still shows a test banner. Old `UseMessageFallback`, `MessageExecutable`, `MessageTimeoutSeconds`, and `InteractiveUser` settings are ignored. Delivery failures are logged without failing the backup.

Native notification delivery uses [Microsoft's Windows Community Toolkit](https://learn.microsoft.com/en-us/dotnet/api/microsoft.toolkit.uwp.notifications.toastcontentbuilder).

## Tray dashboard

The sign-in companion now includes a teal Backuper tray icon. Existing `--install-notifications` shortcuts also start the tray; there is no additional startup application to configure. Windows may initially place the icon inside the hidden-icons menu; drag it beside the clock if you want it always visible.

- Left-click the icon to open a compact status window near the taskbar. Closing the window leaves the tray and service running.
- View the current backup state, transferred bytes and percentage, rclone-reported transfer rate in MiB/s and Mbps, elapsed time and ETA, file/check counts, errors, and successful/failed run totals.
- Read the service's source, destination, daily schedule, configured bandwidth limit, and parallel transfers. These settings are read-only.
- Right-click and choose **Exit (stop service)** to stop the `Backuper` Windows Service, wait for it to stop, and close the companion. Windows may request administrator permission. Cancelling or failing that request keeps the companion open with an error. This does not disable automatic service startup on the next boot.

New backup logs use rclone's `--use-json-log --stats 5s --stats-log-level INFO` output. The service reads the latest statistics incrementally and publishes a small snapshot every two seconds under `Notifications:QueueDirectory/<app-id-hash>/Dashboard/status.json`. This avoids requiring the desktop account to read LocalSystem's private database and logs. No rclone credentials are included. Speed is rclone's reported transfer rate, not total network-adapter traffic; progress totals and ETA may change as rclone discovers files. Old plain-text logs have no structured statistics and display as unavailable. Stale snapshots are labelled and a stopped service never displays an active transfer rate.

Republish and restart both the service and companion to use the dashboard. The tray's stop command targets the service named `Backuper`; IDE-hosted backup processes are separate and are not stopped by it. If the service is not installed, Exit simply closes the tray.

Run regression checks (no real backups or service-stop requests):

```powershell
dotnet run --project Tests/Backuper.Tests.csproj
```

## Run locally

Development run:

```powershell
dotnet run --environment Development
```

Production run from source:

```powershell
dotnet run --environment Production
```

## Publish

Create a production build:

```powershell
dotnet publish Backuper.csproj -c Release -o .\publish
```

Copy your private `appsettings.Production.json` into `.\publish` after publishing and adjust the machine-specific settings. Public releases should contain only the example configuration. Use a new output directory when preparing a release; an old output directory can still contain private files from previous deployments.

When updating from the earlier PowerShell notification implementation, publish to a fresh directory and deploy that output. The old `Services\ShowWindowsToast.ps1` is no longer used or shipped. No antivirus exclusions are required by the application.

## Install as Windows Service

Run PowerShell as Administrator:

```powershell
sc.exe create Backuper binPath= "E:\Path\To\Backuper\publish\Backuper.exe" start= auto
sc.exe start Backuper
```

Check status:

```powershell
sc.exe query Backuper
```

Stop or remove:

```powershell
sc.exe stop Backuper
sc.exe delete Backuper
```

## Notes

- If the service runs as `LocalSystem`, `%USERPROFILE%` resolves to that account profile, not your interactive user profile.
- If you want data under your own user profile, configure the service to run as your Windows user or set `Backup:AppDataDirectory` to an explicit absolute path.
- Service notifications require the desktop companion running in the recipient's signed-in session.
