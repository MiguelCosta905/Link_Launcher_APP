using Newtonsoft.Json;
using ShiftLauncher.Core.Models;

namespace ShiftLauncher.Core.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        _settingsPath = Path.Combine(baseDirectory, "settings.json");
    }

    public async Task<LauncherSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
            return new LauncherSettings();

        var json = await File.ReadAllTextAsync(_settingsPath);
        return JsonConvert.DeserializeObject<LauncherSettings>(json) ?? new LauncherSettings();
    }

    public async Task SaveAsync(LauncherSettings settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(_settingsPath, json);
    }
}
