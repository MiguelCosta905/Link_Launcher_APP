using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using ShiftLauncher.Core.Models;
using ShiftLauncher.Core.ModLoaders;
using ShiftLauncher.Core.Storage;
using ShiftLauncher.Core.Utilities;

namespace ShiftLauncher.Core.Launch;

public sealed class LauncherService
{
    private readonly MinecraftDirectoryService _directoryService;
    private readonly SettingsService _settingsService;
    private readonly JavaService _javaService;
    private readonly ProcessMonitorService _processMonitorService;
    private readonly ModLoaderInstallService _modLoaderInstallService;

    public LauncherService(
        MinecraftDirectoryService directoryService,
        SettingsService settingsService,
        JavaService javaService,
        ProcessMonitorService processMonitorService,
        ModLoaderInstallService modLoaderInstallService)
    {
        _directoryService = directoryService;
        _settingsService = settingsService;
        _javaService = javaService;
        _processMonitorService = processMonitorService;
        _modLoaderInstallService = modLoaderInstallService;
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
        Guard.AgainstNullOrWhiteSpace(settings.SharedGameDirectory, nameof(settings.SharedGameDirectory));

        var path = _directoryService.CreatePath(settings.SharedGameDirectory);
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
                Type = version.Type?.ToString() ?? "Unknown"
            })
            .ToList();
    }

    public async Task<LaunchResult> LaunchOfflineAsync(LaunchRequest request)
    {
        try
        {
            Guard.AgainstNullOrWhiteSpace(request.SharedDirectory, nameof(request.SharedDirectory));
            Guard.AgainstNullOrWhiteSpace(request.InstanceDirectory, nameof(request.InstanceDirectory));

            Directory.CreateDirectory(request.SharedDirectory);
            Directory.CreateDirectory(request.InstanceDirectory);

            var java = string.IsNullOrWhiteSpace(request.JavaPath)
                ? await _javaService.FindBestJavaAsync(request.MinecraftVersion)
                : new JavaInstallation { JavaPath = request.JavaPath, MajorVersion = 0 };

            if (java is null)
            {
                return new LaunchResult
                {
                    Success = false,
                    Message = $"Nao foi encontrado Java compativel com Minecraft {request.MinecraftVersion}."
                };
            }

            var loaderResult = await _modLoaderInstallService.EnsureInstalledAsync(
                request.SharedDirectory,
                request.MinecraftVersion,
                request.VersionName,
                request.ModLoader,
                status =>
                {
                    request.ReportProcessLog($"[Loader] {status}");
                    request.ReportProgress(new LaunchProgress { StatusText = status });
                });

            if (!loaderResult.IsSuccess)
            {
                return new LaunchResult
                {
                    Success = false,
                    Message = loaderResult.Message,
                    Exception = loaderResult.Exception
                };
            }

            var sharedPath = _directoryService.CreatePath(request.SharedDirectory);
            var instancePath = CreateInstancePath(request.InstanceDirectory, sharedPath);
            var launcher = new MinecraftLauncher(sharedPath);

            launcher.FileProgressChanged += (_, e) =>
            {
                request.ReportProgress(new LaunchProgress
                {
                    StatusText = $"A preparar {e.Name}...",
                    CurrentFile = e.Name,
                    FileProgressPercent = GetPercent(e.ProgressedTasks, e.TotalTasks)
                });
            };

            launcher.ByteProgressChanged += (_, e) =>
            {
                request.ReportProgress(new LaunchProgress
                {
                    StatusText = "A descarregar ficheiros...",
                    ByteProgressPercent = e.ToRatio() * 100
                });
            };

            var option = new MLaunchOption
            {
                Path = instancePath,
                JavaPath = java.JavaPath,
                Session = request.Session ?? MSession.CreateOfflineSession(request.PlayerName),
                MaximumRamMb = request.MaximumRamMb,
                GameLauncherName = "ShiftLauncher",
                GameLauncherVersion = "1.0"
            };

            request.ReportProgress(new LaunchProgress
            {
                StatusText = $"A instalar/verificar {request.VersionName}...",
                FileProgressPercent = 0,
                ByteProgressPercent = 0
            });

            var process = await launcher.InstallAndBuildProcessAsync(request.VersionName, option);
            _processMonitorService.AttachOutputLogging(process, request.ReportProcessLog);

            if (!process.Start())
            {
                return new LaunchResult
                {
                    Success = false,
                    Message = "Nao foi possivel iniciar o processo do Minecraft."
                };
            }

            _processMonitorService.BeginReadingOutput(process);
            _ = MonitorExitAsync(process, request);

            return new LaunchResult
            {
                Success = true,
                Message = "Minecraft iniciado.",
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

    private async Task MonitorExitAsync(System.Diagnostics.Process process, LaunchRequest request)
    {
        var result = await _processMonitorService.WaitForExitAsync(process);
        request.ReportProcessExited(result);
        process.Dispose();
    }

    private static MinecraftPath CreateInstancePath(string instanceDirectory, MinecraftPath sharedPath)
    {
        var instancePath = new MinecraftPath(instanceDirectory)
        {
            Library = sharedPath.Library,
            Versions = sharedPath.Versions,
            Resource = sharedPath.Resource,
            Assets = sharedPath.Assets,
            Runtime = sharedPath.Runtime
        };

        instancePath.CreateDirs();
        return instancePath;
    }

    private static double GetPercent(long progressed, long total)
    {
        if (total <= 0)
            return 0;

        return Math.Clamp((double)progressed / total * 100, 0, 100);
    }
}
