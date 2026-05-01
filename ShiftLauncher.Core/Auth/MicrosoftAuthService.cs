using CmlLib.Core.Auth;

namespace ShiftLauncher.Core.Auth;

public sealed class MicrosoftAuthService
{
    public Task<AuthSession> CreateOfflineSessionAsync(string username)
    {
        var session = MSession.CreateOfflineSession(username);

        return Task.FromResult(new AuthSession
        {
            IsOffline = true,
            Username = session.Username,
            Uuid = session.UUID,
            AccessToken = session.AccessToken
        });
    }
}
