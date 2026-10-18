using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Backuper.Services;

public static class NotificationStartup
{
    // One-time, explicit setup using the standard per-user Windows Startup folder.
    public static void Install(string environmentName)
    {
        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot locate the Backuper executable.");
        if (!string.Equals(Path.GetFileName(executable), "Backuper.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Run Backuper.exe --install-notifications from the published installation directory.");
        if (environmentName.Contains('"'))
            throw new InvalidOperationException("The environment name cannot contain quotes.");

        var startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        Directory.CreateDirectory(startup);
        var shortcutPath = Path.Combine(startup, "Backuper Notifications.lnk");
        CreateShortcut(executable, shortcutPath, environmentName);

        // Start now too; the user does not have to log out or open the companion.
        using var process = Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory,
            ArgumentList = { "--notifications", "--environment", environmentName }
        });
    }

    internal static void CreateShortcut(string executable, string shortcutPath, string environmentName)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!;
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            try
            {
                shortcut.TargetPath = executable;
                shortcut.Arguments = $"--notifications --environment \"{environmentName}\"";
                shortcut.WorkingDirectory = Path.GetDirectoryName(executable)!;
                shortcut.Description = "Show Windows notifications from the Backuper service.";
                shortcut.Save();
            }
            finally { Marshal.FinalReleaseComObject(shortcut); }
        }
        finally { Marshal.FinalReleaseComObject(shell); }
    }
}
