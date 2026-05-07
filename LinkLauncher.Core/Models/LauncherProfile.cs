using LinkLauncher.Core.ModLoaders;

namespace LinkLauncher.Core.Models;

public sealed class LauncherProfile
{
    private const string DefaultCoverImagePath = "avares://LinkLauncher.App/Assets/logo.png";

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Nova Instancia";
    public string MinecraftVersion { get; set; } = "latest-release";
    public int MaximumRamMb { get; set; } = 2048;
    public string PlayerName { get; set; } = "Player";
    public ModLoaderProfile ModLoader { get; set; } = new();
    public string? CoverImagePath { get; set; } = "avares://LinkLauncher.App/Assets/logo.png";
    public LauncherProfile Clone(string name)
    {
        return new LauncherProfile
        {
            Name = name,
            MinecraftVersion = MinecraftVersion,
            MaximumRamMb = MaximumRamMb,
            PlayerName = PlayerName,
            CoverImagePath = CoverImagePath,
            ModLoader = new ModLoaderProfile
            {
                LoaderType = ModLoader.LoaderType,
                LoaderVersion = ModLoader.LoaderVersion
            }
        };
    }

    public override string ToString() => Name;
}
