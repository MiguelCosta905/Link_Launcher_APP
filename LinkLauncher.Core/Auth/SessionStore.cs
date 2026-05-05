using Newtonsoft.Json;

namespace LinkLauncher.Core.Auth;

public sealed class SessionStore
{
    private readonly string _sessionPath;

    public SessionStore(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        _sessionPath = Path.Combine(baseDirectory, "session.json");
    }

    public async Task<AuthSession?> LoadAsync()
    {
        if (!File.Exists(_sessionPath))
            return null;

        var json = await File.ReadAllTextAsync(_sessionPath);
        return JsonConvert.DeserializeObject<AuthSession>(json);
    }

    public async Task SaveAsync(AuthSession session)
    {
        var json = JsonConvert.SerializeObject(session, Formatting.Indented);
        await File.WriteAllTextAsync(_sessionPath, json);
    }

    public Task ClearAsync()
    {
        if (File.Exists(_sessionPath))
            File.Delete(_sessionPath);

        return Task.CompletedTask;
    }
}
