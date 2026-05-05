namespace LinkLauncher.Core.ModLoaders;

public sealed class ModLoaderProfile
{
    public LoaderType LoaderType { get; set; } = LoaderType.Vanilla;
    public string? LoaderVersion { get; set; }
}
