using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using CommunityToolkit.Authentication;
using Mail.Models;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using SqlSugar;

namespace Mail.Services
{
    // TODO: Re-Implement OAuthProvider, PCA and MsalProvider only support Microsoft Account
    internal abstract class OAuthMailService : IMailService
    {
        private readonly IPublicClientApplication ClientApplication;
        protected static IMemoryCache MemoryCache => App.Services.GetService<IMemoryCache>()!;
        private IMailService MailServiceImplementation;
        public AccountModel? CurrentAccount { get; set; }
        public abstract MailType MailType { get; }
        public abstract ObservableCollection<MailFolderData> MailFoldersTree { get; }
        protected ISqlSugarClient DbClient { get; }

        public BaseProvider Provider { get; }

        protected abstract string[] Scopes { get; }

        public virtual bool IsSupported => true;

        public virtual bool IsSignIn => Provider.State == ProviderState.SignedIn;

        protected OAuthMailService(WebAccountProviderType Type)
        {
            DbClient = App.Services.GetService<ISqlSugarClient>()!;

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
            await Provider.SignInAsync();
        }

        public async Task<bool> SignInSilentAsync()
        {
            return await Provider.TrySilentSignInAsync();
        }

        public async Task SignOutAsync(IAccount account)
        {
            await Provider.SignOutAsync();
        }

        IMemoryCache IMailService.GetCache()
        {
            return OAuthMailService.MemoryCache;
        }

        public virtual Task<bool> InitSeriviceAsync()
        {
            return Task.FromResult(true);
        }

        public abstract IAsyncEnumerable<MailFolderData> GetMailSuperFoldersAsync(
            CancellationToken CancelToken = default);

        public abstract Task<MailFolderData> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default);

        public abstract IAsyncEnumerable<MailMessageData> GetMailMessageAsync(LoadMailMessageOption RootFolderId,
            CancellationToken CancelToken = default);

        public abstract Task<byte[]?> GetMailMessageFileAttachmentContent(string messageId, string attachmentId);

        public abstract Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default);

        public abstract IAsyncEnumerable<MailMessageFileAttachmentData> GetMailAttachmentFileAsync(
            MailMessageListDetailViewModel model, CancellationToken CancelToken = default);

        public abstract Task LoadAttachmentsAndCacheAsync(string messageId, CancellationToken CancelToken = default);

        /// <summary>
        /// 如果成功, 请将Model的id设置为服务返回的id
        /// </summary>
        /// <param name="Model">执行成功后, Model会被设置id</param>
        /// <returns></returns>
        public abstract Task<bool> MailDraftSaveAsync(MailMessageListDetailViewModel Model);

        public abstract Task<bool> MailSendAsync(MailMessageListDetailViewModel Model);

        public abstract Task<bool> MailReplyAsync(MailMessageListDetailViewModel Model, string ReplyContent,
            bool IsAll = false);

        public abstract Task<bool> MailForwardAsync(MailMessageListDetailViewModel Model, string ForwardContent);

        /// <summary>
        /// 大文件上传(>3mb)
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="BasicProperties"></param>
        /// <param name="StorageFile"></param>
        /// <param name="UploadedSliceCallback"></param>
        /// <param name="CancelToken"></param>
        /// <returns></returns>
        public abstract Task UploadAttachmentSessionAsync(MailMessageListDetailViewModel Model,
            BasicProperties BasicProperties,
            StorageFile StorageFile,
            Action<long> UploadedSliceCallback,
            CancellationToken CancelToken = default);

        public abstract Task<bool> MailMoveAsync(string mailMessageId, string folderId);

        public abstract Task<MailMessageFileAttachmentData?> UploadAttachmentAsync(MailMessageListDetailViewModel Model,
            StorageFile StorageFile, CancellationToken CancelToken = default);

        public abstract Task<bool> MailRemoveAsync(MailMessageListDetailViewModel Model);
    }
}