namespace ShiftLauncher.Core.Launch;

public sealed class LaunchResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ProcessId { get; set; }
    public Exception? Exception { get; set; }
}
