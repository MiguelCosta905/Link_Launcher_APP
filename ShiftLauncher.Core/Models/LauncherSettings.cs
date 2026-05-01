namespace ShiftLauncher.Core.Models;

public sealed class LauncherSettings
{
    public LauncherProfile LastProfile { get; set; } = new();
    public string GameDirectory { get; set; } = ".minecraft";
}
