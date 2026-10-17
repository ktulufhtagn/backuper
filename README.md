# Backuper

Small Windows background service that runs a daily `rclone` backup to a Backblaze-backed remote.

## What it does

- Starts automatically as a Windows Service.
- Checks every 30 minutes whether today's backup has already succeeded.
- Runs the backup after `02:00` local time if no successful backup exists for the current day.
- Runs as soon as the PC is back on if it missed the scheduled time while powered off.
- Stores backup history and last status in SQLite.
- Writes rclone logs and removes logs older than 7 days.
- Sends a Windows notification when a backup starts.

## Prerequisites

- Windows.
- .NET 10 runtime installed on the machine running the service.
- `rclone` installed and available to the service, either:
  - in the system `PATH`, or
  - configured with an absolute path in `Backup:RcloneExecutable`.
- An rclone remote configured for the service account.

Verify rclone manually before installing the service:

```powershell
rclone lsd backup-remote:backups
```

## Configuration

Runtime settings are read from `appsettings.json`, `appsettings.Development.json`, and `appsettings.Production.json`.

`appsettings.Production.json` is intentionally ignored by Git because it is machine-specific.

Default backup settings:

```json
{
  "Backup": {
    "SourcePath": "C:\\Data\\ToBackUp",
    "Destination": "backup-remote:backups",
    "RcloneExecutable": "rclone",
    "Transfers": 4,
    "B2UploadConcurrency": 8,
    "UploadBandwidthLimit": "1Mi",
    "DailyRunTime": "02:00",
    "CheckIntervalMinutes": 30,
    "AppDataDirectory": "%USERPROFILE%\\AppData\\LocalLow\\Backuper",
    "DatabasePath": "backuper.db",
    "LogDirectory": "Logs",
    "LogRetentionDays": 7
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

The service runs the equivalent of:

```powershell
rclone copy C:\Data\ToBackUp backup-remote:backups --progress --transfers 4 --b2-upload-concurrency 8 --bwlimit 1Mi --log-file <log-file> --log-level INFO
```

`--bwlimit 1Mi` limits upload bandwidth to 1 MiB/s so the internet connection remains usable during backup.

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
dotnet publish -c Release -o .\publish
```

Copy or create `appsettings.Production.json` inside `.\publish` and adjust machine-specific settings if needed.

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
- Windows notifications from services can be blocked depending on the service account and interactive session.
