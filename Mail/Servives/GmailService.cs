using CommunityToolkit.Authentication;
using Mail.Class.Data;
using Mail.Class.Models;
using Mail.Enum;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mail.Servives
{
    internal class GmailService : OAuthMailService
    {
        protected override string[] Scopes { get; } = new string[] { "" };

        public GmailService() : base(WebAccountProviderType.Any)
        {

        }

        public override Task<MailFolderData> GetMailFolderAsync(string RootFolderId, CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task<MailFolderData> GetMailFolderAsync(MailFolderType Type, CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type, uint StartIndex = 0, uint Count = 30, CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
