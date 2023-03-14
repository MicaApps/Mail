using Mail.Services.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mail.Services
{
    internal interface IMailService
    {
        public bool IsSupported { get; }

        public bool IsSignIn { get; }

        public Task<bool> InitSeriviceAsync();

        public IAsyncEnumerable<MailFolderData> GetMailFoldersAsync(CancellationToken CancelToken = default);

        public Task<MailFolderDetailData> GetMailFolderDetailAsync(string RootFolderId, CancellationToken CancelToken = default);

        public Task<MailFolderDetailData> GetMailFolderDetailAsync(MailFolderType Type, CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);

        public Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default);
    }
}
