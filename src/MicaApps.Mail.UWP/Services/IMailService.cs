using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Mail.Models;
using Mail.Models.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Mail.Services
{
    internal interface IMailService
    {
        public bool IsSupported { get; }

        public bool IsSignIn { get; }
        AccountModel? CurrentAccount { get; }
        MailType MailType { get; }
        public ObservableCollection<MailFolder> MailFoldersTree { get; }
        IMemoryCache GetCache();

        public Task<bool> InitSeriviceAsync();

        public IAsyncEnumerable<MailFolder> GetMailSuperFoldersAsync(CancellationToken CancelToken = default);

        public Task<MailFolder> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessage> GetMailMessageAsync(LoadMailMessageOption RootFolderId,
            CancellationToken CancelToken = default);

        public Task<byte[]?> GetMailMessageFileAttachmentContent(string messageId, string attachmentId);

        public Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default);

        interface IFocusFilterSupport
        {
            public IAsyncEnumerable<MailMessage> GetMailMessageAsync(LoadMailMessageOption Option,
                CancellationToken CancelToken = default);
        }

        public IAsyncEnumerable<MailMessageFileAttachment> GetMailAttachmentFileAsync(
            MailMessageListDetailViewModel model, CancellationToken CancelToken = default);

        /// <summary>
        /// 如果成功, 请将Model的id设置为服务返回的id
        /// </summary>
        /// <param name="Model">执行成功后, Model会被设置id</param>
        /// <returns></returns>
        Task<bool> MailDraftSaveAsync(MailMessageListDetailViewModel Model);

        Task<bool> MailSendAsync(MailMessageListDetailViewModel Model);
        Task<bool> MailReplyAsync(MailMessageListDetailViewModel Model, string ReplyContent, bool IsAll = false);
        Task<bool> MailForwardAsync(MailMessageListDetailViewModel Model, string ForwardContent);

        Task<MailMessageFileAttachment?> UploadAttachmentAsync(MailMessageListDetailViewModel Model,
            StorageFile StorageFile, CancellationToken CancelToken = default);

        public abstract Task UploadAttachmentSessionAsync(MailMessageListDetailViewModel Model,
            BasicProperties BasicProperties,
            StorageFile StorageFile,
            Action<long> UploadedSliceCallback,
            CancellationToken CancelToken = default);

        Task<bool> MailMoveAsync(string mailMessageId, string folderId);

        Task<bool> MailRemoveAsync(MailMessageListDetailViewModel Model);
    }
}