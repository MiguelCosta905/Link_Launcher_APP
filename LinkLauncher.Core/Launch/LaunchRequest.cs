using CmlLib.Core.Auth;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.ModLoaders;

namespace LinkLauncher.Core.Launch;

public sealed class LaunchRequest
{
    public string MinecraftVersion { get; set; } = "latest-release";
    public string VersionName { get; set; } = "latest-release";
    public string PlayerName { get; set; } = "Player";
    public int MaximumRamMb { get; set; } = 2048;
    public bool UseOfflineMode { get; set; } = true;
    public string SharedDirectory { get; set; } = string.Empty;
    public string InstanceDirectory { get; set; } = string.Empty;
    public ModLoaderProfile ModLoader { get; set; } = new();
    public string? JavaPath { get; set; }
    public MSession? Session { get; set; }
    public Action<LaunchProgress>? ProgressChanged { get; set; }
    public Action<string>? ProcessLogReceived { get; set; }
    public Action<ProcessExitResult>? ProcessExited { get; set; }

    public static LaunchRequest FromSettings(LauncherSettings settings)
    {
        var profile = settings.GetSelectedProfile();
        var instanceDirectory = Path.Combine(settings.SettingsDirectory, "Instances", profile.Id);

        return new LaunchRequest
        {
            MinecraftVersion = profile.MinecraftVersion,
            VersionName = BuildVersionName(profile),
            PlayerName = profile.PlayerName,
            MaximumRamMb = profile.MaximumRamMb,
            SharedDirectory = settings.SharedGameDirectory,
            InstanceDirectory = instanceDirectory,
            UseOfflineMode = true,
            ModLoader = profile.ModLoader
        };
    }

    private static string BuildVersionName(LauncherProfile profile)
    {
        var minecraftVersion = profile.MinecraftVersion;
        var loader = profile.ModLoader;

        if (loader.LoaderType == LoaderType.Vanilla || string.IsNullOrWhiteSpace(loader.LoaderVersion))
            return minecraftVersion;

        return loader.LoaderType switch
        {
            LoaderType.Forge => $"{minecraftVersion}-forge-{loader.LoaderVersion}",
            LoaderType.NeoForge => $"neoforge-{loader.LoaderVersion}",
            LoaderType.Fabric => $"fabric-loader-{loader.LoaderVersion}-{minecraftVersion}",
            LoaderType.Quilt => $"quilt-loader-{loader.LoaderVersion}-{minecraftVersion}",
            _ => minecraftVersion
        };
    }

    public void ReportProgress(LaunchProgress progress) => ProgressChanged?.Invoke(progress);

    public void ReportProcessLog(string message) => ProcessLogReceived?.Invoke(message);

    public void ReportProcessExited(ProcessExitResult result) => ProcessExited?.Invoke(result);
}
