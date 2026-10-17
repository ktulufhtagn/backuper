namespace Backuper.Configuration;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public bool Enabled { get; set; } = true;

    public string AppId { get; set; } = "Backuper";

    public bool UseToast { get; set; } = true;

    public bool UseMessageFallback { get; set; } = true;

    public string MessageExecutable { get; set; } = "msg.exe";

    public int MessageTimeoutSeconds { get; set; } = 20;
}
