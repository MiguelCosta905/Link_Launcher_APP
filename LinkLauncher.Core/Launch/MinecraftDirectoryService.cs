using CmlLib.Core;

namespace LinkLauncher.Core.Launch;

public sealed class MinecraftDirectoryService
{
    public MinecraftPath CreatePath(string baseDirectory)
    {
        var fullPath = Path.GetFullPath(baseDirectory);
        Directory.CreateDirectory(fullPath);
        return new MinecraftPath(fullPath);
    }
}
