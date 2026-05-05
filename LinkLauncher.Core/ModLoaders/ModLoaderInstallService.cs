using System.Diagnostics;
using System.IO.Compression;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using LinkLauncher.Core.Launch;

namespace LinkLauncher.Core.ModLoaders;

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

        if (!IsSupportedLoaderMinecraftVersion(minecraftVersion))
        {
            return ModLoaderInstallResult.Fail(
                $"'{minecraftVersion}' nao e uma versao valida de Minecraft para {modLoader.LoaderType}. " +
                "Seleciona uma versao do jogo, por exemplo 1.21.1.");
        }

        Directory.CreateDirectory(gameDirectory);
        EnsureLauncherProfilesFile(gameDirectory);
        onStatus?.Invoke($"A instalar {modLoader.LoaderType} {modLoader.LoaderVersion}...");

        return modLoader.LoaderType switch
        {
            LoaderType.Fabric => await InstallFabricLikeProfileZipAsync(
                BuildFabricProfileZipUrl(minecraftVersion, modLoader.LoaderVersion),
                gameDirectory,
                versionName,
                onStatus),

            LoaderType.Quilt => await InstallFabricLikeProfileZipAsync(
                BuildQuiltProfileZipUrl(minecraftVersion, modLoader.LoaderVersion),
                gameDirectory,
                versionName,
                onStatus),

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

    // -------------------------------------------------------------------------
    // Verificacao
    // -------------------------------------------------------------------------

    private static bool IsVersionInstalled(string gameDirectory, string versionName)
    {
        var versionJson = Path.Combine(gameDirectory, "versions", versionName, $"{versionName}.json");
        return File.Exists(versionJson);
    }

    private static string? FindInstalledVersionName(string gameDirectory, string expectedVersionName)
    {
        var versionsDirectory = Path.Combine(gameDirectory, "versions");
        if (!Directory.Exists(versionsDirectory))
            return null;

        var exactPath = Path.Combine(versionsDirectory, expectedVersionName, $"{expectedVersionName}.json");
        if (File.Exists(exactPath))
            return expectedVersionName;

        foreach (var jsonPath in Directory.EnumerateFiles(versionsDirectory, "*.json", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(jsonPath);
            if (string.Equals(name, expectedVersionName, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // launcher_profiles.json
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Fabric / Quilt (profile ZIP)
    // -------------------------------------------------------------------------

    private async Task<ModLoaderInstallResult> InstallFabricLikeProfileZipAsync(
        string url,
        string gameDirectory,
        string versionName,
        Action<string>? onStatus)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), $"fabricprofile-{Guid.NewGuid():N}.zip");
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"fabricprofile-{Guid.NewGuid():N}");

        try
        {
            onStatus?.Invoke("A descarregar perfil do Fabric/Quilt...");

            await using (var input = await _httpClient.GetStreamAsync(url))
            await using (var output = File.Create(tempZip))
                await input.CopyToAsync(output);

            onStatus?.Invoke("A extrair perfil...");
            ZipFile.ExtractToDirectory(tempZip, tempDirectory, overwriteFiles: true);

            onStatus?.Invoke("A copiar ficheiros do perfil...");
            CopyFabricLikeProfile(tempDirectory, gameDirectory, versionName);

            var installed = FindInstalledVersionName(gameDirectory, versionName);
            if (installed is null)
            {
                var presentVersions = ListVersionsInstalled(gameDirectory);
                return ModLoaderInstallResult.Fail(
                    $"Perfil {versionName} nao encontrado apos instalacao. " +
                    $"Versoes presentes: {string.Join(", ", presentVersions)}");
            }

            return ModLoaderInstallResult.Success();
        }
        catch (HttpRequestException ex)
        {
            return ModLoaderInstallResult.Fail(
                $"Nao foi possivel descarregar o perfil ZIP para {versionName}. " +
                "Verifica a versao de Minecraft/loader selecionada.",
                ex);
        }
        catch (Exception ex)
        {
            return ModLoaderInstallResult.Fail(
                $"Nao foi possivel instalar {versionName}: {ex.Message}", ex);
        }
        finally
        {
            TryDelete(tempZip);
            TryDeleteDirectory(tempDirectory);
        }
    }

    // -------------------------------------------------------------------------
    // Forge / NeoForge (installer JAR)
    // -------------------------------------------------------------------------

    private async Task<ModLoaderInstallResult> InstallForgeInstallerAsync(
        string installerUrl,
        string gameDirectory,
        string minecraftVersion,
        string versionName,
        Action<string>? onStatus)
    {
        var java = await _javaService.FindBestJavaAsync(minecraftVersion);
        if (java is null)
            return ModLoaderInstallResult.Fail(
                $"Nao foi encontrado Java compativel com Minecraft {minecraftVersion}.");

        var installerFileName = Path.GetFileName(new Uri(installerUrl).LocalPath);
        var installerPath = Path.Combine(Path.GetTempPath(), installerFileName);

        try
        {
            onStatus?.Invoke($"A preparar Minecraft {minecraftVersion} para o installer...");
            var vanillaResult = await EnsureVanillaInstalledAsync(gameDirectory, minecraftVersion, java.JavaPath);
            if (!vanillaResult.IsSuccess)
                return vanillaResult;

            onStatus?.Invoke("A descarregar installer do loader...");
            await using (var input = await _httpClient.GetStreamAsync(installerUrl))
            await using (var output = File.Create(installerPath))
                await input.CopyToAsync(output);

            onStatus?.Invoke("A executar installer do loader (isto pode demorar alguns minutos)...");
            var result = await RunInstallerAsync(java.JavaPath, installerPath, gameDirectory);

            if (result.ExitCode != 0)
            {
                return ModLoaderInstallResult.Fail(
                    $"O installer terminou com codigo {result.ExitCode}." +
                    Environment.NewLine + result.Output);
            }

            var installed = FindInstalledVersionName(gameDirectory, versionName);
            if (installed is null)
            {
                var presentVersions = ListVersionsInstalled(gameDirectory);
                return ModLoaderInstallResult.Fail(
                    $"O installer terminou sem erro, mas o perfil {versionName} nao foi encontrado. " +
                    $"Versoes presentes: {string.Join(", ", presentVersions)}");
            }

            return ModLoaderInstallResult.Success();
        }
        catch (Exception ex)
        {
            return ModLoaderInstallResult.Fail(
                $"Nao foi possivel instalar o loader: {ex.Message}", ex);
        }
        finally
        {
            TryDelete(installerPath);
        }
    }

    // -------------------------------------------------------------------------
    // Vanilla (pre-requisito do Forge)
    // -------------------------------------------------------------------------

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

            try { process.Dispose(); }
            catch { /* processo ainda nao iniciado */ }

            return IsVersionInstalled(gameDirectory, minecraftVersion)
                ? ModLoaderInstallResult.Success()
                : ModLoaderInstallResult.Fail(
                    $"Nao foi possivel preparar Minecraft {minecraftVersion} para instalar o loader.");
        }
        catch (Exception ex)
        {
            return ModLoaderInstallResult.Fail(
                $"Nao foi possivel preparar Minecraft {minecraftVersion}: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // Utilidades
    // -------------------------------------------------------------------------

    private static IReadOnlyList<string> ListVersionsInstalled(string gameDirectory)
    {
        var versionsDirectory = Path.Combine(gameDirectory, "versions");
        if (!Directory.Exists(versionsDirectory))
            return [];

        return Directory
            .EnumerateDirectories(versionsDirectory)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .ToList()!;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var dir in Directory.EnumerateDirectories(
                     sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDirectory, dir);
            Directory.CreateDirectory(Path.Combine(targetDirectory, rel));
        }

        foreach (var file in Directory.EnumerateFiles(
                     sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDirectory, file);
            var dest = Path.Combine(targetDirectory, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static void CopyFabricLikeProfile(
        string extractedDirectory,
        string gameDirectory,
        string versionName)
    {
        var versionsInTemp = Path.Combine(extractedDirectory, "versions");
        if (Directory.Exists(versionsInTemp))
        {
            CopyDirectory(extractedDirectory, gameDirectory);
            return;
        }

        var nestedRoot = Directory.GetDirectories(extractedDirectory)
            .FirstOrDefault(d => Directory.Exists(Path.Combine(d, "versions")));

        if (nestedRoot is not null)
        {
            CopyDirectory(nestedRoot, gameDirectory);
            return;
        }

        var profileDirectory = Directory.GetDirectories(extractedDirectory)
            .FirstOrDefault(d =>
                File.Exists(Path.Combine(d, $"{Path.GetFileName(d)}.json")) ||
                File.Exists(Path.Combine(d, $"{versionName}.json")));

        if (profileDirectory is null &&
            File.Exists(Path.Combine(extractedDirectory, $"{versionName}.json")))
        {
            profileDirectory = extractedDirectory;
        }

        if (profileDirectory is null)
        {
            CopyDirectory(extractedDirectory, gameDirectory);
            return;
        }

        var targetVersionDirectory = Path.Combine(gameDirectory, "versions", versionName);
        CopyDirectory(profileDirectory, targetVersionDirectory);
    }

    private static bool IsSupportedLoaderMinecraftVersion(string minecraftVersion)
    {
        return minecraftVersion.Length >= 3 &&
               minecraftVersion.StartsWith("1.", StringComparison.Ordinal) &&
               minecraftVersion[2..].All(c => c == '.' || char.IsDigit(c));
    }

    private static async Task<(int ExitCode, string Output)> RunInstallerAsync(
        string javaPath,
        string installerPath,
        string gameDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = javaPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = gameDirectory
        };

        startInfo.ArgumentList.Add("-jar");
        startInfo.ArgumentList.Add(installerPath);
        startInfo.ArgumentList.Add("--installClient");
        startInfo.ArgumentList.Add(gameDirectory);

        using var process = Process.Start(startInfo);
        if (process is null)
            return (-1, "Nao foi possivel iniciar o installer.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask  = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await outputTask + Environment.NewLine + await errorTask);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* ignora */ }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { /* ignora */ }
    }

    // -------------------------------------------------------------------------
    // URLs
    // -------------------------------------------------------------------------

    private static string BuildFabricProfileZipUrl(string minecraftVersion, string loaderVersion) =>
        $"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(minecraftVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/zip";

    private static string BuildQuiltProfileZipUrl(string minecraftVersion, string loaderVersion) =>
        $"https://meta.quiltmc.org/v3/versions/loader/{Uri.EscapeDataString(minecraftVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/zip";

    private static string BuildForgeInstallerUrl(string minecraftVersion, string loaderVersion)
    {
        var fullVersion = $"{minecraftVersion}-{loaderVersion}";
        return $"https://maven.minecraftforge.net/net/minecraftforge/forge/{fullVersion}/forge-{fullVersion}-installer.jar";
    }

    private static string BuildNeoForgeInstallerUrl(string loaderVersion) =>
        $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{loaderVersion}/neoforge-{loaderVersion}-installer.jar";
}

public sealed class ModLoaderInstallResult
{
    public bool IsSuccess { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public Exception? Exception { get; private init; }

    public static ModLoaderInstallResult Success() =>
        new() { IsSuccess = true };

    public static ModLoaderInstallResult Fail(string message, Exception? exception = null) =>
        new() { IsSuccess = false, Message = message, Exception = exception };
}
