using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using XboxAuthNet.Game.Msal;
using XboxAuthNet.Game.Msal.OAuth;

namespace ShiftLauncher.Core.Auth;

public sealed class MicrosoftAuthService
{
    private const string ClientId = "499c8d36-be2a-4231-9ebd-ef291b7bb64c";

    public async Task<MSession> LoginWithDeviceCodeAsync(Func<string, Task> showDeviceCodeMessage)
    {
        var app = await MsalClientHelper.BuildApplicationWithCache(ClientId);
        var loginHandler = new JELoginHandlerBuilder().Build();

        var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();

        authenticator.AddMsalOAuth(app, msal => msal.DeviceCode(code =>
        {
            return showDeviceCodeMessage(code.Message);
        }));

        authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
        authenticator.AddJEAuthenticator();

        return await authenticator.ExecuteForLauncherAsync();
    }

    public Task<AuthSession> CreateOfflineSessionAsync(string username)
    {
        var session = MSession.CreateOfflineSession(username);

        return Task.FromResult(new AuthSession
        {
            IsOffline = true,
            Username = session.Username ?? username,
            Uuid = session.UUID,
            AccessToken = session.AccessToken
        });
    }
}
