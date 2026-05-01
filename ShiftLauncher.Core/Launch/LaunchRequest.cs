using ShiftLauncher.Core.Models;

namespace ShiftLauncher.Core.Launch;

public sealed class LaunchRequest
{
    public string VersionName { get; set; } = "latest-release";
    public string PlayerName { get; set; } = "Player";
    public int MaximumRamMb { get; set; } = 4096;
    public bool UseOfflineMode { get; set; } = true;
    public string GameDirectory { get; set; } = string.Empty;
    public Action<LaunchProgress>? ProgressChanged { get; set; }

    public static LaunchRequest FromSettings(LauncherSettings settings)
    {
        return new LaunchRequest
        {
            VersionName = settings.LastProfile.MinecraftVersion,
            PlayerName = settings.LastProfile.PlayerName,
            MaximumRamMb = settings.LastProfile.MaximumRamMb,
            GameDirectory = settings.GameDirectory,
            UseOfflineMode = true
        };
    }

    public void ReportProgress(LaunchProgress progress)
    {
        ProgressChanged?.Invoke(progress);
    }
}
