using System.Diagnostics;
using System.Text.RegularExpressions;
using ShiftLauncher.Core.Models;

namespace ShiftLauncher.Core.Launch;

public sealed class JavaService
{
    public async Task<JavaInstallation?> FindBestJavaAsync(string minecraftVersion)
    {
        var requiredMajorVersion = GetRequiredJavaMajorVersion(minecraftVersion);
        var installations = await FindInstalledJavaAsync();

        return installations
            .Where(java => java.MajorVersion >= requiredMajorVersion)
            .OrderBy(java => java.MajorVersion)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<JavaInstallation>> FindInstalledJavaAsync()
    {
        var candidates = new List<string>();

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
            candidates.Add(Path.Combine(javaHome, "bin", "java.exe"));

        candidates.AddRange(await FindJavaFromPathAsync());

        var result = new List<JavaInstallation>();

        foreach (var javaPath in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(javaPath))
                continue;

            var info = await ReadJavaInfoAsync(javaPath);
            if (info is not null)
                result.Add(info);
        }

        return result;
    }

    public int GetRequiredJavaMajorVersion(string minecraftVersion)
    {
        if (!TryParseMinecraftVersion(minecraftVersion, out var major, out var minor, out var patch))
            return 17;

        if (major > 1)
            return 25;

        if (minor <= 16)
            return 8;

        if (minor == 17)
            return 16;

        if (minor > 26)
            return 25;

        if (minor == 26 && patch >= 1)
            return 25;

        if (minor == 20 && patch >= 5)
            return 21;

        if (minor >= 21)
            return 21;

        return 17;
    }

    private static async Task<IReadOnlyList<string>> FindJavaFromPathAsync()
    {
        var paths = new List<string>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "where",
            Arguments = "java",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            return paths;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        foreach (var line in output.Split(Environment.NewLine))
        {
            var path = line.Trim();
            if (path.EndsWith("java.exe", StringComparison.OrdinalIgnoreCase))
                paths.Add(path);
        }

        return paths;
    }

    private static async Task<JavaInstallation?> ReadJavaInfoAsync(string javaPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = javaPath,
            Arguments = "-version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            return null;

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var versionText = output + Environment.NewLine + error;
        var majorVersion = ParseJavaMajorVersion(versionText);

        if (majorVersion <= 0)
            return null;

        return new JavaInstallation
        {
            DisplayName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(javaPath))) ?? "Java",
            JavaPath = javaPath,
            MajorVersion = majorVersion,
            Is64Bit = versionText.Contains("64-Bit", StringComparison.OrdinalIgnoreCase)
                || versionText.Contains("64-bit", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static int ParseJavaMajorVersion(string versionText)
    {
        var match = Regex.Match(versionText, "version \"(?<major>\\d+)(\\.(?<minor>\\d+))?");
        if (!match.Success)
            return 0;

        var major = int.Parse(match.Groups["major"].Value);

        if (major == 1 && int.TryParse(match.Groups["minor"].Value, out var legacyMajor))
            return legacyMajor;

        return major;
    }

    private static bool TryParseMinecraftVersion(
        string version,
        out int major,
        out int minor,
        out int patch)
    {
        major = 0;
        minor = 0;
        patch = 0;

        var match = Regex.Match(version, "^(?<major>\\d+)\\.(?<minor>\\d+)(\\.(?<patch>\\d+))?");
        if (!match.Success)
            return false;

        major = int.Parse(match.Groups["major"].Value);
        minor = int.Parse(match.Groups["minor"].Value);

        if (match.Groups["patch"].Success)
            patch = int.Parse(match.Groups["patch"].Value);

        return true;
    }
}
