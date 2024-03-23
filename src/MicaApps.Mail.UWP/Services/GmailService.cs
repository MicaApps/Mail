using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Authentication;
using Mail.Models;
using Mail.Models.Enums;
using Mail.ViewModels;

namespace Mail.Services
{
    abstract class GmailService : OAuthMailService
    {
        protected override string[] Scopes { get; } = new string[] { "" };

        public GmailService() : base(WebAccountProviderType.Any)
        {
        }

        public override MailType MailType => MailType.Gmail;

        public override IAsyncEnumerable<MailFolder> GetMailSuperFoldersAsync(
            CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task<MailFolder> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override IAsyncEnumerable<MailMessage> GetMailMessageAsync(LoadMailMessageOption RootFolderId,
            CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override IAsyncEnumerable<MailMessageFileAttachment> GetMailAttachmentFileAsync(
            MailMessageListDetailViewModel model, CancellationToken cancelToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> MailDraftSaveAsync(MailMessageListDetailViewModel Model)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> MailSendAsync(MailMessageListDetailViewModel Model)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> MailReplyAsync(MailMessageListDetailViewModel Model, string ReplyContent, bool IsAll)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> MailForwardAsync(MailMessageListDetailViewModel Model, string ForwardContent)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> MailMoveAsync(string mailMessageId, string folderId)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> MailRemoveAsync(MailMessageListDetailViewModel Model)
        {
            throw new System.NotImplementedException();
        }
    }
}