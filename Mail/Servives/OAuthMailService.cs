using CommunityToolkit.Authentication;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Mail.Servives
{
    // TODO: Re-Implement OAuthProvider, PCA and MsalProvider only support Microsoft Account
    internal abstract class OAuthMailService : IMailService
    {

        private BaseProvider provider;

        public BaseProvider Provider { get => provider; }

        private IPublicClientApplication application;

        private string[] scopes;

        public OAuthMailService(string[] scopes, WebAccountProviderConfig config)
        {
            var builder = PublicClientApplicationBuilder.Create(config.ClientId)
                .WithClientName("MailService")
                .WithClientVersion("1.0.0")
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithDefaultRedirectUri()
                .WithBroker(true);
            application = builder.Build();
            this.scopes= scopes;
            this.provider = new MsalProvider(application, scopes);
            this.provider.StateChanged += Provider_StateChanged;
        }

        private void Provider_StateChanged(object sender, ProviderStateChangedEventArgs e)
        {
            Trace.WriteLine($"AuthService: {e.NewState}");
        }

        public async Task SignIn()
        {
            //Workaround for MsalProvider sign-in not pop up
            var account = (await application.GetAccountsAsync()).FirstOrDefault();
            await application.AcquireTokenInteractive(scopes)
                .WithAccount(account)
                .ExecuteAsync();
            await provider.TrySilentSignInAsync();
        }

        public async Task<bool> SignInSilent()
        {
            return await Provider.TrySilentSignInAsync();
        }

        public async Task SignOut(IAccount account)
        {
            await Provider.SignOutAsync();
        }

        public Task<bool> InitSerivice() { return Task.FromResult(true); }

        public bool IsSupported() => true;

        public bool IsSignIn() => provider.State == ProviderState.SignedIn;

    }
}
