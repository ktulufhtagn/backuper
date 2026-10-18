using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backuper.Configuration;
using Microsoft.Extensions.Options;

namespace Backuper.Services;

// The service writes plain notification data. The desktop companion only reads it;
// no commands, executable paths, or code are accepted through this channel.
public sealed class NotificationQueue(IOptions<NotificationOptions> options)
{
    public string DirectoryPath => Path.Combine(
        Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.Value.QueueDirectory)),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.AppId)))[..16]);

    public async Task EnqueueAsync(string message, CancellationToken cancellationToken)
    {
        var directory = DirectoryPath;
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, $"{Guid.NewGuid():N}.json");
        var temporary = destination + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary,
                JsonSerializer.Serialize(new Notification(DateTimeOffset.UtcNow, message)), cancellationToken);
            File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-7))
                File.Delete(file);
        }
    }

    public sealed record Notification(DateTimeOffset CreatedAt, string Message);
}
