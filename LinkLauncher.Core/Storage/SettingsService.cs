using LinkLauncher.Core.Models;
using LinkLauncher.Core.ModLoaders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinkLauncher.Core.Storage;

public sealed class SettingsService
{
    private readonly string _settingsPath;
    private readonly string _baseDirectory;

    public SettingsService(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        _baseDirectory = baseDirectory;
        _settingsPath = Path.Combine(baseDirectory, FilePaths.SettingsFileName);
    }

    public async Task<LauncherSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
            return CreateDefaultSettings();

        var json = await File.ReadAllTextAsync(_settingsPath);
        var settings = JsonConvert.DeserializeObject<LauncherSettings>(json);
        return Normalize(MigrateLegacySettings(json, settings));
    }

    public async Task SaveAsync(LauncherSettings settings)
    {
        settings = Normalize(settings);
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    private LauncherSettings CreateDefaultSettings()
    {
        return LauncherSettings.CreateDefault(
            _baseDirectory,
            FilePaths.GetGameDirectory());
    }

    private LauncherSettings MigrateLegacySettings(string json, LauncherSettings? settings)
    {
        settings ??= CreateDefaultSettings();

        try
        {
            var root = JObject.Parse(json);

            if (string.IsNullOrWhiteSpace(settings.SharedGameDirectory))
            {
                var legacyGameDirectory = root.Value<string>("GameDirectory");
                if (!string.IsNullOrWhiteSpace(legacyGameDirectory))
                    settings.SharedGameDirectory = legacyGameDirectory;
            }

            if (settings.Profiles.Count == 0 && root["LastProfile"] is JObject lastProfile)
            {
                var profile = lastProfile.ToObject<LauncherProfile>() ?? new LauncherProfile();
                if (string.IsNullOrWhiteSpace(profile.Name))
                    profile.Name = "Instância Principal";

                settings.Profiles.Add(profile);
                settings.SelectedProfileId = profile.Id;
            }
        }
        catch (JsonException)
        {
            // The normalization below creates safe defaults if the file is incomplete.
        }

        return settings;
    }

    private LauncherSettings Normalize(LauncherSettings? settings)
    {
        settings ??= CreateDefaultSettings();

        settings.SettingsDirectory = string.IsNullOrWhiteSpace(settings.SettingsDirectory)
            ? _baseDirectory
            : settings.SettingsDirectory;

        settings.SharedGameDirectory = string.IsNullOrWhiteSpace(settings.SharedGameDirectory)
            ? FilePaths.GetGameDirectory()
            : settings.SharedGameDirectory;

        settings.ThemeMode = NormalizeThemeMode(settings.ThemeMode);
        settings.LanguageCode = NormalizeLanguageCode(settings.LanguageCode);

        if (settings.Profiles.Count == 0)
        {
            settings.Profiles.Add(new LauncherProfile { Name = "Instância Principal" });
            settings.SelectedProfileId = settings.Profiles[0].Id;
        }

        foreach (var profile in settings.Profiles)
            NormalizeProfile(profile);

        if (settings.Profiles.All(profile => profile.Id != settings.SelectedProfileId))
            settings.SelectedProfileId = settings.Profiles[0].Id;

        return settings;
    }

    private static void NormalizeProfile(LauncherProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
            profile.Id = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(profile.Name))
            profile.Name = "Instância";

        profile.ModLoader ??= new ModLoaderProfile();
        profile.MinecraftVersion = ExtractVanillaVersion(profile.MinecraftVersion);

        if (string.IsNullOrWhiteSpace(profile.MinecraftVersion))
            profile.MinecraftVersion = "latest-release";

        profile.MaximumRamMb = NormalizeRam(profile.MaximumRamMb);

        if (string.IsNullOrWhiteSpace(profile.PlayerName))
            profile.PlayerName = "Player";
            
        if (string.IsNullOrWhiteSpace(profile.CoverImagePath))
        profile.CoverImagePath = "avares://LinkLauncher.App/Assets/logo.png";

    }

    private static string ExtractVanillaVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        if (version.StartsWith("fabric-loader-", StringComparison.OrdinalIgnoreCase) ||
            version.StartsWith("quilt-loader-", StringComparison.OrdinalIgnoreCase))
        {
            var parts = version.Split('-');
            var last = parts[^1];
            if (IsVanillaVersion(last))
                return last;
        }

        var forgeIdx = version.IndexOf("-forge-", StringComparison.OrdinalIgnoreCase);
        if (forgeIdx > 0)
        {
            var candidate = version[..forgeIdx];
            if (IsVanillaVersion(candidate))
                return candidate;
        }

        if (version.StartsWith("neoforge-", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (IsVanillaVersion(version) || version.StartsWith("latest-", StringComparison.OrdinalIgnoreCase))
            return version;

        return string.Empty;
    }

    private static bool IsVanillaVersion(string version)
    {
        return version.Length >= 3 &&
               version.StartsWith("1.", StringComparison.Ordinal) &&
               version[2..].All(c => c == '.' || char.IsDigit(c));
    }

    private static string NormalizeThemeMode(string? themeMode)
    {
        return themeMode switch
        {
            "Light" or "Claro" => "Light",
            "Dark" or "Escuro" => "Dark",
            _ => "System"
        };
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return languageCode switch
        {
            "en" => "en",
            "pt-PT" => "pt-PT",
            _ => "pt-PT"
        };
    }

    private static int NormalizeRam(int maximumRamMb)
    {
        const int minRamMb = 1024;
        const int maxRamMb = 16384;
        const int stepMb = 1024;

        if (maximumRamMb <= 0)
            return 2048;

        var clamped = Math.Clamp(maximumRamMb, minRamMb, maxRamMb);
        var rounded = (int)Math.Round((double)clamped / stepMb, MidpointRounding.AwayFromZero);
        return rounded * stepMb;
    }
}
