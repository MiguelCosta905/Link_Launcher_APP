namespace LinkLauncher.Core.Storage;

public static class FilePaths
{
    public const string AppName = "LinkLauncher";
    public const string GameFolderName = "GameData";
    public const string SettingsFileName = "settings.json";

    public static string GetAppDataDirectory(string appName = AppName)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);

        Directory.CreateDirectory(root);
        return root;
    }

    public static string GetGameDirectory(string appName = AppName)
    {
        var gameDirectory = Path.Combine(GetAppDataDirectory(appName), GameFolderName);
        Directory.CreateDirectory(gameDirectory);
        return gameDirectory;
    }

    public static string GetSettingsFilePath(string appName = AppName)
    {
        return Path.Combine(GetAppDataDirectory(appName), SettingsFileName);
    }
}
