namespace ShiftLauncher.Core.Models;

public sealed class MinecraftVersionItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public override string ToString() => $"{Name} ({Type})";
}
