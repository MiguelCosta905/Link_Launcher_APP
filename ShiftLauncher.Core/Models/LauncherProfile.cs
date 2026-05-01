namespace ShiftLauncher.Core.Models;

public sealed class LauncherProfile
{
    public string MinecraftVersion { get; set; } = "latest-release";
    public int MaximumRamMb { get; set; } = 4096;
    public string PlayerName { get; set; } = "Player";
}
