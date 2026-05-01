namespace ShiftLauncher.Core.Launch;

public sealed class LaunchProgress
{
    public string StatusText { get; set; } = string.Empty;
    public string? CurrentFile { get; set; }
    public double FileProgressPercent { get; set; }
    public double ByteProgressPercent { get; set; }
}
