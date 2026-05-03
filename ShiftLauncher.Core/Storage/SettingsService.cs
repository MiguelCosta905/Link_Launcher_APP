using Newtonsoft.Json;
using ShiftLauncher.Core.Models;
using ShiftLauncher.Core.ModLoaders;

namespace ShiftLauncher.Core.Storage;

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
        return Normalize(settings);
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

    private LauncherSettings Normalize(LauncherSettings? settings)
    {
        settings ??= CreateDefaultSettings();

        settings.SettingsDirectory = string.IsNullOrWhiteSpace(settings.SettingsDirectory)
            ? _baseDirectory
            : settings.SettingsDirectory;

        settings.GameDirectory = string.IsNullOrWhiteSpace(settings.GameDirectory)
            ? FilePaths.GetGameDirectory()
            : settings.GameDirectory;

        settings.LastProfile ??= new LauncherProfile();
        settings.LastProfile.ModLoader ??= new ModLoaderProfile();

        if (string.IsNullOrWhiteSpace(settings.LastProfile.MinecraftVersion))
            settings.LastProfile.MinecraftVersion = "latest-release";

        if (settings.LastProfile.MaximumRamMb <= 0)
            settings.LastProfile.MaximumRamMb = 4096;

        if (string.IsNullOrWhiteSpace(settings.LastProfile.PlayerName))
            settings.LastProfile.PlayerName = "Player";

        return settings;
    }
}
