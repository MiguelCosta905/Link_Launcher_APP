namespace ShiftLauncher.Core.Launch;

public sealed class ProcessExitResult
{
    public int ExitCode { get; set; }
    public bool Crashed => ExitCode != 0;
    public string Message { get; set; } = string.Empty;
}
