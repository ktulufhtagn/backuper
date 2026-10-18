using Backuper;
using Backuper.Configuration;
using Backuper.Data;
using Backuper.Services;
using Microsoft.EntityFrameworkCore;
using Backuper.Desktop;

// The elevated helper has a fixed operation and never starts the backup host.
if (args.Contains("--stop-service", StringComparer.OrdinalIgnoreCase))
{
    try { BackuperServiceControl.Stop(); }
    catch { Environment.ExitCode = 1; }
    return;
}

// The toolkit's activation switch must not start another backup scheduler.
// Do not initialize the desktop notification library inside the service session.
if (args.Contains("-ToastActivated", StringComparer.OrdinalIgnoreCase))
    return;

var desktopNotifications = args.Contains("--notifications", StringComparer.OrdinalIgnoreCase);
var testNotification = args.Contains("--test-notification", StringComparer.OrdinalIgnoreCase);
var installNotifications = args.Contains("--install-notifications", StringComparer.OrdinalIgnoreCase);
args = args.Where(arg => !string.Equals(arg, "--notifications", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(arg, "--test-notification", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(arg, "--install-notifications", StringComparison.OrdinalIgnoreCase)).ToArray();

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddWindowsService(options => options.ServiceName = "Backuper");

builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection(BackupOptions.SectionName));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));
builder.Services.AddSingleton<NotificationQueue>();

if (desktopNotifications || testNotification || installNotifications)
{
    using var process = System.Diagnostics.Process.GetCurrentProcess();
    if (process.SessionId == 0)
        throw new InvalidOperationException("Run the notification companion in your signed-in desktop session.");
    if (installNotifications)
    {
        NotificationStartup.Install(builder.Environment.EnvironmentName);
        return;
    }
    if (testNotification)
    {
        WindowsToastNotifier.ShowToast("Native Windows notifications are working.");
        return;
    }
    // One companion per desktop session prevents duplicate banners.
    using var mutex = new Mutex(true, @"Local\Backuper.NotificationCompanion", out var firstInstance);
    if (!firstInstance) return;
    builder.Services.AddHostedService<NotificationDesktopWorker>();
    using var desktopHost = builder.Build();
    await desktopHost.StartAsync();
    try { await BackuperTray.RunAsync(desktopHost.Services.GetRequiredService<NotificationQueue>()); }
    finally { await desktopHost.StopAsync(); }
    return;
}
builder.Services.AddSingleton<IBackupPathProvider, BackupPathProvider>();

builder.Services.AddDbContext<BackupDbContext>((serviceProvider, options) =>
{
    var pathProvider = serviceProvider.GetRequiredService<IBackupPathProvider>();
    options.UseSqlite($"Data Source={pathProvider.DatabasePath}");
});

builder.Services.AddSingleton<ILogRetentionService, LogRetentionService>();
builder.Services.AddSingleton<IWindowsNotifier, WindowsToastNotifier>();
builder.Services.AddScoped<IBackupExecutor, RcloneBackupExecutor>();
builder.Services.AddScoped<IBackupScheduler, BackupScheduler>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<BackupDashboardPublisher>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<BackupDbContext>();
    await database.Database.MigrateAsync();
    await database.MarkInterruptedRunsAsInterruptedAsync();
}

host.Run();
