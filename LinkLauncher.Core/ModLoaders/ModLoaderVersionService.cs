using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace LinkLauncher.Core.ModLoaders;

public sealed class ModLoaderVersionService
{
    private readonly HttpClient _httpClient = new();

    public async Task<IReadOnlyList<string>> GetLoaderVersionsAsync(
        LoaderType loaderType,
        string minecraftVersion)
    {
        return loaderType switch
        {
            LoaderType.Vanilla => [],
            LoaderType.Fabric => await GetFabricVersionsAsync(minecraftVersion),
            LoaderType.Forge => await GetForgeVersionsAsync(minecraftVersion),
            LoaderType.NeoForge => await GetNeoForgeVersionsAsync(minecraftVersion),
            LoaderType.Quilt => await GetQuiltVersionsAsync(),
            _ => []
        };
    }

    public async Task<IReadOnlySet<string>> GetSupportedMinecraftVersionsAsync(LoaderType loaderType)
    {
        return loaderType switch
        {
            LoaderType.Vanilla => new HashSet<string>(),
            LoaderType.Fabric => await GetFabricMinecraftVersionsAsync(),
            LoaderType.Forge => await GetForgeMinecraftVersionsAsync(),
            LoaderType.NeoForge => await GetNeoForgeMinecraftVersionsAsync(),
            LoaderType.Quilt => await GetFabricMinecraftVersionsAsync(),
            _ => new HashSet<string>()
        };
    }

    private async Task<IReadOnlyList<string>> GetFabricVersionsAsync(string minecraftVersion)
    {
        var url = $"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(minecraftVersion)}";
        var entries = await _httpClient.GetFromJsonAsync<List<FabricLoaderEntry>>(url);

        return entries?
            .Select(x => x.Loader.Version)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList()
            ?? [];
    }

    private async Task<IReadOnlyList<string>> GetForgeVersionsAsync(string minecraftVersion)
    {
        var versions = await GetMavenVersionsAsync(
            "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml");

        return versions
            .Where(x => x.StartsWith($"{minecraftVersion}-", StringComparison.OrdinalIgnoreCase))
            .Select(x => x[(minecraftVersion.Length + 1)..])
            .Reverse()
            .ToList();
    }

    private async Task<IReadOnlyList<string>> GetNeoForgeVersionsAsync(string minecraftVersion)
    {
        var versions = await GetMavenVersionsAsync(
            "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml");

        var prefix = ToNeoForgePrefix(minecraftVersion);

        return versions
            .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Reverse()
            .ToList();
    }

    private async Task<IReadOnlyList<string>> GetQuiltVersionsAsync()
    {
        var versions = await GetMavenVersionsAsync(
            "https://maven.quiltmc.org/repository/release/org/quiltmc/quilt-loader/maven-metadata.xml");

        return versions.Reverse().ToList();
    }

    private async Task<IReadOnlySet<string>> GetFabricMinecraftVersionsAsync()
    {
        var entries = await _httpClient.GetFromJsonAsync<List<FabricGameVersion>>(
            "https://meta.fabricmc.net/v2/versions/game");

        return entries?
            .Where(x => x.Stable)
            .Select(x => x.Version)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>();
    }

    private async Task<IReadOnlySet<string>> GetForgeMinecraftVersionsAsync()
    {
        var versions = await GetMavenVersionsAsync(
            "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml");

        return versions
            .Select(x => x.Split('-')[0])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlySet<string>> GetNeoForgeMinecraftVersionsAsync()
    {
        var versions = await GetMavenVersionsAsync(
            "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml");

        return versions
            .Select(ToMinecraftVersionFromNeoForge)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<string>> GetMavenVersionsAsync(string url)
    {
        var xml = await _httpClient.GetStringAsync(url);
        var document = XDocument.Parse(xml);

        return document
            .Descendants("version")
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string ToNeoForgePrefix(string minecraftVersion)
    {
        return minecraftVersion.StartsWith("1.", StringComparison.Ordinal)
            ? minecraftVersion[2..] + "."
            : minecraftVersion + ".";
    }

    private static string ToMinecraftVersionFromNeoForge(string neoForgeVersion)
    {
        var parts = neoForgeVersion.Split('.');
        if (parts.Length < 2)
            return string.Empty;

        return $"1.{parts[0]}.{parts[1]}";
    }

    private sealed class FabricLoaderEntry
    {
        [JsonPropertyName("loader")]
        public FabricLoaderInfo Loader { get; set; } = new();
    }

    private sealed class FabricLoaderInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    private sealed class FabricGameVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("stable")]
        public bool Stable { get; set; }
    }
}
