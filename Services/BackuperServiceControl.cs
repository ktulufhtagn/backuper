using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;

namespace Backuper.Services;

public static class BackuperServiceControl
{
    public static string Status()
    {
        try { using var service = new ServiceController("Backuper"); return service.Status.ToString(); }
        catch (InvalidOperationException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 1060 })
        { return "Not installed"; }
        catch { return "Unavailable"; }
    }

    public static void Stop()
    {
        using var service = new ServiceController("Backuper");
        try
        {
            if (service.Status == ServiceControllerStatus.Stopped) return;
            if (service.Status != ServiceControllerStatus.StopPending) service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
        }
        catch (InvalidOperationException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 1060 })
        { /* Already uninstalled. */ }
    }

    public static async Task StopWithElevationAsync()
    {
        try { await Task.Run(Stop); return; }
        catch (Exception ex) when (IsAccessDenied(ex)) { }

        var executable = Environment.ProcessPath!;
        if (!string.Equals(Path.GetFileName(executable), "Backuper.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the published Backuper.exe to request service-stop permission.");
        using var helper = Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden,
            ArgumentList = { "--stop-service" }
        }) ?? throw new InvalidOperationException("Could not start the service-stop request.");
        await helper.WaitForExitAsync();
        if (helper.ExitCode != 0 || Status() is not ("Stopped" or "Not installed"))
            throw new InvalidOperationException("The service did not stop. Check Windows service permissions and try again.");
    }

    private static bool IsAccessDenied(Exception exception) =>
        exception is Win32Exception { NativeErrorCode: 5 } ||
        exception.InnerException is not null && IsAccessDenied(exception.InnerException);
}
