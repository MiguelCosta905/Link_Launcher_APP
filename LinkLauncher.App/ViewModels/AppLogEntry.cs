using System;

namespace LinkLauncher.App.ViewModels;

public sealed class AppLogEntry
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"[{CreatedAt:HH:mm:ss}] {Level}: {Message}";
    }
}
