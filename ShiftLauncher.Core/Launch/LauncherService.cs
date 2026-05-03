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
    private readonly JavaService _javaService;
    private readonly ProcessMonitorService _processMonitorService;
    
    public LauncherService(
    MinecraftDirectoryService directoryService,
    SettingsService settingsService,
    JavaService javaService,
    ProcessMonitorService processMonitorService)
    {
        _directoryService = directoryService;
        _settingsService = settingsService;
        _javaService = javaService;
        _processMonitorService = processMonitorService;
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

launcher.FileProgressChanged += (_, e) =>
{
    request.ReportProgress(new LaunchProgress
    {
        StatusText = $"{e.EventType}: {e.Name}",
        CurrentFile = e.Name,
        FileProgressPercent = GetPercent(e.ProgressedTasks, e.TotalTasks)
    });
};

launcher.ByteProgressChanged += (_, e) =>
{
    request.ReportProgress(new LaunchProgress
    {
        StatusText = "Downloading files...",
        ByteProgressPercent = GetPercent(e.ProgressedBytes, e.TotalBytes)
    });
};

var java = await _javaService.FindBestJavaAsync(request.VersionName);

if (java is null)
{
    return new LaunchResult
    {
        Success = false,
        Message = $"No compatible Java installation found for Minecraft {request.VersionName}."
    };
}

request.JavaPath = java.JavaPath;
request.ReportProgress(new LaunchProgress
{
    StatusText = $"Using Java {java.MajorVersion}: {java.JavaPath}"
});

var session = request.Session ?? MSession.CreateOfflineSession(request.PlayerName);

var option = new MLaunchOption
{
    MaximumRamMb = request.MaximumRamMb,
    JavaPath = request.JavaPath,
    Session = session
};


        var process = await launcher.InstallAndBuildProcessAsync(request.VersionName, option);

        process.EnableRaisingEvents = true;
        process.Start();

    _ = Task.Run(async () =>
    {
        var exitResult = await _processMonitorService.WaitForExitAsync(process);
        request.ReportProcessExited(exitResult);
    });


            return new LaunchResult
            {
                Success = true,
                Message = "Minecraft launcLaunchOfflineAsynched successfully.",
                ProcessId = process.Id
            };
        }
        catch (Exception ex)
        {
            return new LaunchResult
            {
                Success = false,
                Message = ErrorMessageService.ToUserMessage(ex),
                Exception = ex
            };

        }
    }
    private static double GetPercent(long progressed, long total)
    {
        if (total <= 0)
            return 0;

        return Math.Clamp((double)progressed / total * 100, 0, 100);
    }
}
