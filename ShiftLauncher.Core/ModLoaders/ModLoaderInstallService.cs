using System.Diagnostics;
using System.IO.Compression;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using ShiftLauncher.Core.Launch;

namespace ShiftLauncher.Core.ModLoaders;

public sealed class ModLoaderInstallService
{
    private readonly HttpClient _httpClient = new();
    private readonly JavaService _javaService;

    public ModLoaderInstallService(JavaService javaService)
    {
        _javaService = javaService;
    }

    public async Task<ModLoaderInstallResult> EnsureInstalledAsync(
        string gameDirectory,
        string minecraftVersion,
        string versionName,
        ModLoaderProfile modLoader,
        Action<string>? onStatus = null)
    {
        if (modLoader.LoaderType == LoaderType.Vanilla)
            return ModLoaderInstallResult.Success();

        if (IsVersionInstalled(gameDirectory, versionName))
            return ModLoaderInstallResult.Success();

        if (string.IsNullOrWhiteSpace(modLoader.LoaderVersion))
            return ModLoaderInstallResult.Fail($"Define a versao do {modLoader.LoaderType} antes de jogar.");

        Directory.CreateDirectory(gameDirectory);
        EnsureLauncherProfilesFile(gameDirectory);
        onStatus?.Invoke($"A instalar {modLoader.LoaderType} {modLoader.LoaderVersion}...");

        return modLoader.LoaderType switch
        {
            LoaderType.Fabric => await InstallFabricLikeProfileZipAsync(
                BuildFabricProfileZipUrl(minecraftVersion, modLoader.LoaderVersion),
                gameDirectory,
                versionName),

            LoaderType.Quilt => await InstallFabricLikeProfileZipAsync(
                BuildQuiltProfileZipUrl(minecraftVersion, modLoader.LoaderVersion),
                gameDirectory,
                versionName),

            LoaderType.Forge => await InstallForgeInstallerAsync(
                BuildForgeInstallerUrl(minecraftVersion, modLoader.LoaderVersion),
                gameDirectory,
                minecraftVersion,
                versionName,
                onStatus),

            LoaderType.NeoForge => await InstallForgeInstallerAsync(
                BuildNeoForgeInstallerUrl(modLoader.LoaderVersion),
                gameDirectory,
                minecraftVersion,
                versionName,
                onStatus),

            _ => ModLoaderInstallResult.Fail($"Loader nao suportado: {modLoader.LoaderType}")
        };
    }

    private static bool IsVersionInstalled(string gameDirectory, string versionName)
    {
        var versionJson = Path.Combine(gameDirectory, "versions", versionName, $"{versionName}.json");
        return File.Exists(versionJson);
    }

    private static void EnsureLauncherProfilesFile(string gameDirectory)
    {
        var profilePath = Path.Combine(gameDirectory, "launcher_profiles.json");
        if (File.Exists(profilePath))
            return;

        File.WriteAllText(
            profilePath,
            """
            {
              "profiles": {},
              "settings": {},
              "version": 3
            }
            """);
    }

    private async Task<ModLoaderInstallResult> InstallFabricLikeProfileZipAsync(
        string url,
        string gameDirectory,
        string versionName)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), $"{versionName}.zip");
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"{versionName}-{Guid.NewGuid():N}");

        try
        {
            await using (var input = await _httpClient.GetStreamAsync(url))
            await using (var output = File.Create(tempZip))
                await input.CopyToAsync(output);

            ZipFile.ExtractToDirectory(tempZip, tempDirectory, overwriteFiles: true);
            CopyDirectory(tempDirectory, gameDirectory);

            var installedVersion = FindInstalledVersionName(gameDirectory, versionName);
            return installedVersion is not null
                ? ModLoaderInstallResult.Success()
                : ModLoaderInstallResult.Fail($"A instalacao terminou, mas o perfil {versionName} nao foi encontrado.");
        }
        catch (HttpRequestException ex)
        {
            return ModLoaderInstallResult.Fail(
                $"Nao foi encontrado profile ZIP para {versionName}. Escolhe outra versao de Minecraft/loader ou instala o Quilt manualmente por enquanto.",
                ex);
        }
        catch (Exception ex)
        {
            return ModLoaderInstallResult.Fail($"Nao foi possivel instalar {versionName}: {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(tempZip))
                File.Delete(tempZip);

            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private async Task<ModLoaderInstallResult> InstallForgeInstallerAsync(
        string installerUrl,
        string gameDirectory,
        string minecraftVersion,
        string versionName,
        Action<string>? onStatus)
    {
        var java = await _javaService.FindBestJavaAsync(minecraftVersion);
        if (java is null)
            return ModLoaderInstallResult.Fail($"Nao foi encontrado Java compativel com Minecraft {minecraftVersion}.");

        var installerPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(installerUrl));

        try
        {
            onStatus?.Invoke($"A preparar Minecraft {minecraftVersion} para o installer...");
            var vanillaResult = await EnsureVanillaInstalledAsync(gameDirectory, minecraftVersion, java.JavaPath);
            if (!vanillaResult.IsSuccess)
                return vanillaResult;

            await using (var input = await _httpClient.GetStreamAsync(installerUrl))
            await using (var output = File.Create(installerPath))
                await input.CopyToAsync(output);

            onStatus?.Invoke("A executar installer do loader...");
            var result = await RunInstallerAsync(java.JavaPath, installerPath, gameDirectory);
            if (result.ExitCode != 0)
            {
                return ModLoaderInstallResult.Fail(
                    $"O installer terminou com codigo {result.ExitCode}.{Environment.NewLine}{result.Output}");
            }

            return IsVersionInstalled(gameDirectory, versionName) || FindInstalledVersionName(gameDirectory, versionName) is not null
                ? ModLoaderInstallResult.Success()
                : ModLoaderInstallResult.Fail($"O installer terminou sem erro, mas o perfil {versionName} nao foi encontrado.");
        }
        catch (Exception ex)
        {
            return ModLoaderInstallResult.Fail($"Nao foi possivel instalar o loader: {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(installerPath))
                File.Delete(installerPath);
        }
    }

    private static async Task<ModLoaderInstallResult> EnsureVanillaInstalledAsync(
        string gameDirectory,
        string minecraftVersion,
        string javaPath)
    {
        if (IsVersionInstalled(gameDirectory, minecraftVersion))
            return ModLoaderInstallResult.Success();

        try
        {
            var path = new MinecraftPath(gameDirectory);
            var launcher = new MinecraftLauncher(path);
            var option = new MLaunchOption
            {
                JavaPath = javaPath,
                Session = MSession.CreateOfflineSession("InstallerCheck"),
                MaximumRamMb = 1024
            };

            var process = await launcher.InstallAndBuildProcessAsync(minecraftVersion, option);

            try
            {
                process.Dispose();
            }
            catch
            {
                // Nothing to clean if CmlLib returns an unstarted process that is already disposed.
            }

            return IsVersionInstalled(gameDirectory, minecraftVersion)
                ? ModLoaderInstallResult.Success()
                : ModLoaderInstallResult.Fail($"Nao foi possivel preparar Minecraft {minecraftVersion} para instalar o loader.");
        }
        catch (Exception ex)
        {
            return ModLoaderInstallResult.Fail($"Nao foi possivel preparar Minecraft {minecraftVersion}: {ex.Message}", ex);
        }
    }

    private static string? FindInstalledVersionName(string gameDirectory, string expectedVersionName)
    {
        var versionsDirectory = Path.Combine(gameDirectory, "versions");
        if (!Directory.Exists(versionsDirectory))
            return null;

        foreach (var jsonPath in Directory.EnumerateFiles(versionsDirectory, "*.json", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(jsonPath);
            if (string.Equals(name, expectedVersionName, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return null;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(file, targetPath, overwrite: true);
        }
    }

    private static async Task<(int ExitCode, string Output)> RunInstallerAsync(
        string javaPath,
        string installerPath,
        string gameDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = javaPath,
            Arguments = $"-jar \"{installerPath}\" --installClient \"{gameDirectory}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            return (-1, "Nao foi possivel iniciar o installer.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await outputTask + Environment.NewLine + await errorTask);
    }

    private static string BuildFabricProfileZipUrl(string minecraftVersion, string loaderVersion)
    {
        return $"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(minecraftVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/zip";
    }

    private static string BuildQuiltProfileZipUrl(string minecraftVersion, string loaderVersion)
    {
        return $"https://meta.quiltmc.org/v3/versions/loader/{Uri.EscapeDataString(minecraftVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/zip";
    }

    private static string BuildForgeInstallerUrl(string minecraftVersion, string loaderVersion)
    {
        var fullVersion = $"{minecraftVersion}-{loaderVersion}";
        return $"https://maven.minecraftforge.net/net/minecraftforge/forge/{fullVersion}/forge-{fullVersion}-installer.jar";
    }

    private static string BuildNeoForgeInstallerUrl(string loaderVersion)
    {
        return $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{loaderVersion}/neoforge-{loaderVersion}-installer.jar";
    }
}

public sealed class ModLoaderInstallResult
{
    public bool IsSuccess { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public Exception? Exception { get; private init; }

    public static ModLoaderInstallResult Success()
    {
        return new ModLoaderInstallResult { IsSuccess = true };
    }

    public static ModLoaderInstallResult Fail(string message, Exception? exception = null)
    {
        return new ModLoaderInstallResult
        {
            IsSuccess = false,
            Message = message,
            Exception = exception
        };
    }
}
