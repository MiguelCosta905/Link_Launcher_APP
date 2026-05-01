namespace ShiftLauncher.Core.Models;

public sealed class LauncherSettings
{
    public string LauncherName { get; set; } = "Shift Launcher";
    public string SettingsDirectory { get; set; } = string.Empty;
    public LauncherProfile LastProfile { get; set; } = new();
    public string GameDirectory { get; set; } = string.Empty;

    public static LauncherSettings CreateDefault(string settingsDirectory, string gameDirectory)
    {
        return new LauncherSettings
        {
            SettingsDirectory = settingsDirectory,
            GameDirectory = gameDirectory,
            LastProfile = new LauncherProfile()
        };
    }
}
