using ShiftLauncher.Core.Models;

namespace ShiftLauncher.Core.Launch;

public sealed class JavaService
{
    public Task<JavaInstallation?> DetectDefaultJavaAsync()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (string.IsNullOrWhiteSpace(javaHome))
            return Task.FromResult<JavaInstallation?>(null);

        var javaExe = Path.Combine(javaHome, "bin", "java.exe");
        if (!File.Exists(javaExe))
            return Task.FromResult<JavaInstallation?>(null);

        return Task.FromResult<JavaInstallation?>(new JavaInstallation
        {
            DisplayName = "JAVA_HOME",
            JavaPath = javaExe
        });
    }
}
