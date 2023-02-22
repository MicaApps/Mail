using Mail.Class.Data;
using Mail.Class.Models;
using Mail.Enum;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mail.Servives.Interface
{
    internal interface IMailService
    {
        public bool IsSupported { get; }

        public bool IsSignIn { get; }

        public Task<bool> InitSeriviceAsync();

        public Task<MailFolderData> GetMailFolderAsync(string RootFolderId, CancellationToken CancelToken = default);

        public Task<MailFolderData> GetMailFolderAsync(MailFolderType Type, CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);

        public IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default);

        public Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default);
    }
}
