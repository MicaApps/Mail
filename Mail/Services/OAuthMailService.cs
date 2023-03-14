using CommunityToolkit.Authentication;
using Mail.Services.Data;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mail.Services
{
    // TODO: Re-Implement OAuthProvider, PCA and MsalProvider only support Microsoft Account
    internal abstract class OAuthMailService : IMailService
    {
        private readonly IPublicClientApplication ClientApplication;

        public BaseProvider Provider { get; }

        protected abstract string[] Scopes { get; }

        public virtual bool IsSupported => true;

        public virtual bool IsSignIn => Provider.State == ProviderState.SignedIn;

        protected OAuthMailService(WebAccountProviderType Type)
        {
            ClientApplication = PublicClientApplicationBuilder.Create(Secrect.AadClientId)
                .WithClientName("MailService")
                .WithClientVersion("1.0.0")
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithDefaultRedirectUri()
                .WithBroker(true)
                .Build();

            Provider = new MsalProvider(ClientApplication, Scopes);
            Provider.StateChanged += Provider_StateChanged;
        }

        private void Provider_StateChanged(object sender, ProviderStateChangedEventArgs e)
        {
            Trace.WriteLine($"AuthService: {e.NewState}");
        }

        public async Task SignInAsync()
        {
            //Workaround for MsalProvider sign-in not pop up
            var account = (await ClientApplication.GetAccountsAsync()).FirstOrDefault();
            await ClientApplication.AcquireTokenInteractive(Scopes).WithAccount(account).ExecuteAsync();
            await Provider.TrySilentSignInAsync();
        }

        public async Task<bool> SignInSilentAsync()
        {
            return await Provider.TrySilentSignInAsync();
        }

        public async Task SignOutAsync(IAccount account)
        {
            await Provider.SignOutAsync();
        }

        public virtual Task<bool> InitSeriviceAsync()
        {
            return Task.FromResult(true);
        }

        public abstract IAsyncEnumerable<MailFolderData> GetMailFoldersAsync(CancellationToken CancelToken = default);

        public abstract Task<MailFolderDetailData> GetMailFolderDetailAsync(string RootFolderId, CancellationToken CancelToken = default);

        public abstract Task<MailFolderDetailData> GetMailFolderDetailAsync(MailFolderType Type, CancellationToken CancelToken = default);

        public abstract IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);

        public abstract IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);

        public abstract Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default);
    }
}
