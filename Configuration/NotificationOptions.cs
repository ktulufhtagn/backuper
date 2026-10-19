namespace Backuper.Configuration;

/// <summary>Configures native Windows notifications and the shared service-to-desktop queue.</summary>
public sealed class NotificationOptions
{
    /// <summary>Configuration section containing notification settings.</summary>
    public const string SectionName = "Notifications";

    /// <summary>Whether backup events are delivered as notifications.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Queue namespace shared by the service and companion; the toast library manages Windows app identity separately.</summary>
    public string AppId { get; set; } = "Backuper";

    /// <summary>Whether native toast delivery is enabled.</summary>
    public bool UseToast { get; set; } = true;

    /// <summary>Shared root for notification queues and dashboard snapshots; the service needs write access and desktop users need read access.</summary>
    public string QueueDirectory { get; set; } = @"%ProgramData%\Backuper\Notifications";
}
