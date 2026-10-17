namespace Backuper.Configuration;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public bool Enabled { get; set; } = true;

    public string AppId { get; set; } = "Backuper";
}
