using Backuper;
using Backuper.Configuration;
using Backuper.Data;
using Backuper.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "Backuper");

builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection(BackupOptions.SectionName));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));
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

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<BackupDbContext>();
    await database.Database.MigrateAsync();
    await database.MarkInterruptedRunsAsFailedAsync();
}

host.Run();
