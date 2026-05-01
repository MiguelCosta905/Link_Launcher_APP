using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using ShiftLauncher.Core.Models;
using ShiftLauncher.Core.Storage;
using ShiftLauncher.Core.Utilities;

namespace ShiftLauncher.Core.Launch;

public sealed class LauncherService
{
    private readonly MinecraftDirectoryService _directoryService;
    private readonly SettingsService _settingsService;

    public LauncherService(
        MinecraftDirectoryService directoryService,
        SettingsService settingsService)
    {
        _directoryService = directoryService;
        _settingsService = settingsService;
    }

    public Task<LauncherSettings> LoadSettingsAsync()
    {
        return _settingsService.LoadAsync();
    }

    public Task SaveSettingsAsync(LauncherSettings settings)
    {
        return _settingsService.SaveAsync(settings);
    }

    public MinecraftLauncher CreateLauncher(LauncherSettings settings)
    {
        Guard.AgainstNullOrWhiteSpace(settings.GameDirectory, nameof(settings.GameDirectory));

        var path = _directoryService.CreatePath(settings.GameDirectory);
        return new MinecraftLauncher(path);
    }

    public LaunchRequest CreateLaunchRequest(LauncherSettings settings)
    {
        return LaunchRequest.FromSettings(settings);
    }

    public async Task<IReadOnlyList<MinecraftVersionItem>> GetVersionsAsync(LauncherSettings settings)
    {
        var launcher = CreateLauncher(settings);
        var versions = await launcher.GetAllVersionsAsync();

        return versions
            .Select(version => new MinecraftVersionItem
            {
                Name = version.Name,
                Type = version.Type.ToString()
            })
            .ToList();
    }

    public async Task<LaunchResult> LaunchOfflineAsync(LaunchRequest request)
    {
        try
        {
            Guard.AgainstNullOrWhiteSpace(request.VersionName, nameof(request.VersionName));
            Guard.AgainstNullOrWhiteSpace(request.PlayerName, nameof(request.PlayerName));
            Guard.AgainstNullOrWhiteSpace(request.GameDirectory, nameof(request.GameDirectory));

            var path = _directoryService.CreatePath(request.GameDirectory);
            var launcher = new MinecraftLauncher(path);

            var option = new MLaunchOption
            {
                MaximumRamMb = request.MaximumRamMb,
                Session = MSession.CreateOfflineSession(request.PlayerName)
            };

            var process = await launcher.InstallAndBuildProcessAsync(request.VersionName, option);
            process.Start();

            return new LaunchResult
            {
                Success = true,
                Message = "Minecraft launched successfully.",
                ProcessId = process.Id
            };
        }
        catch (Exception ex)
        {
            return new LaunchResult
            {
                Success = false,
                Message = ex.Message,
                Exception = ex
            };
        }
    }
}
