using System.IO;
using System.Linq;

namespace LinkLauncher.Core.Models;

public sealed class LauncherSettings
{
    public string LauncherName { get; set; } = "Link Launcher";
    public string SettingsDirectory { get; set; } = string.Empty;
    public string SharedGameDirectory { get; set; } = string.Empty;
    public string ThemeMode { get; set; } = "System";
    public string LanguageCode { get; set; } = "pt-PT";
    public List<LauncherProfile> Profiles { get; set; } = new();
    public string? SelectedProfileId { get; set; }

    public LauncherProfile? GetSelectedProfile()
    {
        if (Profiles.Count == 0)
            return null;

        var selected = Profiles.FirstOrDefault(p => p.Id == SelectedProfileId);
        if (selected is not null)
            return selected;

        SelectedProfileId = Profiles[0].Id;
        return Profiles[0];
    }

    public string GetInstanceDirectory(LauncherProfile profile)
    {
        return Path.Combine(SettingsDirectory, "Instances", EnsureInstanceFolderName(profile));
    }

    public static LauncherSettings CreateDefault(string settingsDirectory, string sharedGameDirectory)
    {
        return new LauncherSettings
        {
            SettingsDirectory = settingsDirectory,
            SharedGameDirectory = sharedGameDirectory
        };
    }

    private string EnsureInstanceFolderName(LauncherProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.InstanceFolderName))
            return profile.InstanceFolderName;

        var baseName = SanitizeDirectoryName(profile.Name);

        var hasDuplicateName = Profiles.Count(p =>
            string.Equals(SanitizeDirectoryName(p.Name), baseName, StringComparison.OrdinalIgnoreCase)) > 1;

        if (!hasDuplicateName)
        {
            profile.InstanceFolderName = baseName;
            return profile.InstanceFolderName;
        }

        var suffix = profile.Id.Length >= 6 ? profile.Id[..6] : profile.Id;
        profile.InstanceFolderName = $"{baseName}-{suffix}";
        return profile.InstanceFolderName;
    }

    private static string SanitizeDirectoryName(string? name)
    {
        var source = string.IsNullOrWhiteSpace(name) ? "Instancia" : name.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(source.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray())
            .Trim()
            .TrimEnd('.');

        return string.IsNullOrWhiteSpace(cleaned) ? "Instancia" : cleaned;
    }
}
