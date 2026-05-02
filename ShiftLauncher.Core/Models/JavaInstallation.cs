namespace ShiftLauncher.Core.Models;

public sealed class JavaInstallation
{
    public string DisplayName { get; set; } = string.Empty;
    public string JavaPath { get; set; } = string.Empty;
    public int MajorVersion { get; set; }
    public bool Is64Bit { get; set; }

    public override string ToString()
    {
        return $"{DisplayName} - Java {MajorVersion}";
    }
}
