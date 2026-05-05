using System.Diagnostics;

namespace LinkLauncher.Core.Launch;

public sealed class ProcessMonitorService
{
    public void AttachOutputLogging(Process process, Action<string> onLog)
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                onLog(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                onLog(e.Data);
        };
    }

    public async Task<ProcessExitResult> WaitForExitAsync(
        Process process,
        CancellationToken cancellationToken = default)
    {
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessExitResult
        {
            ExitCode = process.ExitCode,
            Message = process.ExitCode == 0
                ? "Minecraft closed normally."
                : $"Minecraft closed with exit code {process.ExitCode}."
        };
    }

    public void BeginReadingOutput(Process process)
    {
        if (process.StartInfo.RedirectStandardOutput)
            process.BeginOutputReadLine();

        if (process.StartInfo.RedirectStandardError)
            process.BeginErrorReadLine();
    }
}
