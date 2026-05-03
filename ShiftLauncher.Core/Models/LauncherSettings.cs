namespace ShiftLauncher.Core.Models;

public sealed class LauncherSettings
{
    public string LauncherName { get; set; } = "Link Launcher";
    public string SettingsDirectory { get; set; } = string.Empty;
    public string SharedGameDirectory { get; set; } = string.Empty;
    public string ThemeMode { get; set; } = "Sistema";
    public List<LauncherProfile> Profiles { get; set; } = new();
    public string? SelectedProfileId { get; set; }

    public LauncherProfile GetSelectedProfile()
    {
        if (Profiles.Count == 0)
        {
            var defaultProfile = new LauncherProfile { Name = "Instancia Principal" };
            Profiles.Add(defaultProfile);
            SelectedProfileId = defaultProfile.Id;
        }

        var selected = Profiles.FirstOrDefault(p => p.Id == SelectedProfileId);
        if (selected is not null)
            return selected;

        SelectedProfileId = Profiles[0].Id;
        return Profiles[0];
    }

    public static LauncherSettings CreateDefault(string settingsDirectory, string sharedGameDirectory)
    {
        var settings = new LauncherSettings
        {
            SettingsDirectory = settingsDirectory,
            SharedGameDirectory = sharedGameDirectory
        };

        settings.GetSelectedProfile();
        return settings;
    }
}
