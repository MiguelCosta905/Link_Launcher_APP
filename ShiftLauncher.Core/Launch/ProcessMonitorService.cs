using System.Diagnostics;

namespace ShiftLauncher.Core.Launch;

public sealed class ProcessMonitorService
{
    public Task WaitForExitAsync(Process process, CancellationToken cancellationToken = default)
    {
        return process.WaitForExitAsync(cancellationToken);
    }
}
