namespace LinkLauncher.Core.Auth;

public sealed class AuthSession
{
    public bool IsOffline { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    public string? AccessToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
