using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mail.Models;
using Mail.Services.Data;
using Microsoft.Graph;

namespace Mail.Services
{
    internal interface IMailService
    {
        public bool IsSupported { get; }

        public bool IsSignIn { get; }

        public Task<bool> InitSeriviceAsync();

        public IAsyncEnumerable<MailFolderData> GetMailFoldersAsync(CancellationToken CancelToken = default);

        public Task<MailFolderDetailData> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default);

        public Task<MailFolderDetailData> GetMailFolderDetailAsync(MailFolderType Type,
            CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, uint StartIndex = 0,
            uint Count = 30, CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type, uint StartIndex = 0,
            uint Count = 30, CancellationToken CancelToken = default);

        public Task<byte[]> GetMailMessageFileAttachmentContent(string messageId, string attachmentId);

        public Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default);

        interface IFocusFilterSupport
        {
            public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, bool focused,
                uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);
        }

        public Task<IMessageAttachmentsCollectionPage> GetMailAttachmentFileAsync(MailMessageListDetailViewModel model);
    }
}