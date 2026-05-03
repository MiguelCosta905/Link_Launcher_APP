using System;
using CmlLib.Core.Auth;
using ShiftLauncher.Core.Models;
using ShiftLauncher.Core.ModLoaders;

namespace ShiftLauncher.Core.Launch;

public sealed class LaunchRequest
{
    public string MinecraftVersion { get; set; } = "latest-release";
    public string VersionName { get; set; } = "latest-release";
    public string PlayerName { get; set; } = "Player";
    public int MaximumRamMb { get; set; } = 4096;
    public bool UseOfflineMode { get; set; } = true;
    public string GameDirectory { get; set; } = string.Empty;
    public ModLoaderProfile ModLoader { get; set; } = new();
    public string? JavaPath { get; set; }
    public MSession? Session { get; set; }
    public Action<LaunchProgress>? ProgressChanged { get; set; }
    public Action<string>? ProcessLogReceived { get; set; }
    public Action<ProcessExitResult>? ProcessExited { get; set; }

    public static LaunchRequest FromSettings(LauncherSettings settings)
    {
        return new LaunchRequest
        {
            MinecraftVersion = settings.LastProfile.MinecraftVersion,
            VersionName = BuildVersionName(settings.LastProfile),
            PlayerName = settings.LastProfile.PlayerName,
            MaximumRamMb = settings.LastProfile.MaximumRamMb,
            GameDirectory = settings.GameDirectory,
            UseOfflineMode = true,
            ModLoader = settings.LastProfile.ModLoader
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

    public void ReportProgress(LaunchProgress progress)
    {
        ProgressChanged?.Invoke(progress);
    }

    public void ReportProcessLog(string message)
    {
        ProcessLogReceived?.Invoke(message);
    }

    public void ReportProcessExited(ProcessExitResult result)
    {
        ProcessExited?.Invoke(result);
    }
}
