
using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using Mail.Services.Data;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mail.Services
{
    internal class OutlookService : OAuthMailService
    {
        protected override string[] Scopes { get; } = new string[]
        {
            "User.Read",
            "User.ReadBasic.All",
            "People.Read",
            "MailboxSettings.Read",
            "Calendars.ReadWrite",
            "Contacts.Read",
            "Contacts.ReadWrite",
            "Mail.ReadWrite",
            "offline_access"
        };

        public OutlookService() : base(WebAccountProviderType.Msa)
        {

        }

        public override async Task<MailFolderData> GetMailFolderAsync(MailFolderType Type, CancellationToken CancelToken = default)
        {
            IMailFolderRequestBuilder Builder = Type switch
            {
                MailFolderType.Inbox => Provider.GetClient().Me.MailFolders.Inbox,
                MailFolderType.Drafts => Provider.GetClient().Me.MailFolders.Drafts,
                MailFolderType.SentItems => Provider.GetClient().Me.MailFolders.SentItems,
                MailFolderType.Deleted => Provider.GetClient().Me.MailFolders.DeletedItems,
                _ => throw new NotSupportedException()
            };

            return await GetMailFolderAsync((await Builder.Request().GetAsync()).Id, CancelToken);
        }

        public override async Task<MailFolderData> GetMailFolderAsync(string RootFolderId, CancellationToken CancelToken = default)
        {
            async IAsyncEnumerable<MailFolderData> GenerateSubMailFolderDataBuilder(IMailFolderRequestBuilder Builder, [EnumeratorCancellation] CancellationToken CancelToken = default)
            {
                foreach (MailFolder SubFolder in await Builder.ChildFolders.Request().GetAsync(CancelToken))
                {
                    CancelToken.ThrowIfCancellationRequested();
                    IMailFolderRequestBuilder SubFolderBuilder = Builder.ChildFolders[SubFolder.Id];
                    yield return new MailFolderData(SubFolder.Id, Convert.ToUInt32(SubFolder.TotalItemCount), await GenerateSubMailFolderDataBuilder(SubFolderBuilder, CancelToken).ToArrayAsync());
                }
            }

            IMailFolderRequestBuilder Builder = Provider.GetClient().Me.MailFolders[RootFolderId];
            MailFolder MailFolder = await Builder.Request().GetAsync(CancelToken);
            return new MailFolderData(MailFolder.Id, Convert.ToUInt32(MailFolder.TotalItemCount), await GenerateSubMailFolderDataBuilder(Builder, CancelToken).ToArrayAsync(CancelToken));
        }

        public override async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type, uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            IMailFolderRequestBuilder Builder = Type switch
            {
                MailFolderType.Inbox => Provider.GetClient().Me.MailFolders.Inbox,
                MailFolderType.Drafts => Provider.GetClient().Me.MailFolders.Drafts,
                MailFolderType.SentItems => Provider.GetClient().Me.MailFolders.SentItems,
                MailFolderType.Deleted => Provider.GetClient().Me.MailFolders.DeletedItems,
                _ => throw new NotSupportedException()
            };

            await foreach (MailMessageData Data in GetMailMessageAsync((await Builder.Request().GetAsync(CancelToken)).Id, StartIndex, Count, CancelToken))
            {
                yield return Data;
            }
        }

        public override async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            IMailFolderMessagesCollectionRequestBuilder Builder = Provider.GetClient().Me.MailFolders[RootFolderId].Messages;

            foreach (Message Message in await Builder.Request().Skip((int)StartIndex).Top((int)Count).GetAsync(CancelToken))
            {
                CancelToken.ThrowIfCancellationRequested();

                yield return new MailMessageData(Message.Subject,
                                                 new MailMessageRecipientData(Message.Sender.EmailAddress.Name, Message.Sender.EmailAddress.Address),
                                                 Message.ToRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 Message.CcRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 Message.BccRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 new MailMessageContentData(Message.Body.Content, Message.BodyPreview, (MailMessageContentType)Message.Body.ContentType),
                                                 Message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name, Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size), Attachment.LastModifiedDateTime.GetValueOrDefault())) ?? Enumerable.Empty<MailMessageAttachmentData>());
            }
        }

        public override async Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default)
        {
            IUserContactsCollectionPage Contacts = await Provider.GetClient().Me.Contacts.Request().GetAsync(CancelToken);
            return Contacts.Select((Contact) => Contact.EmailAddresses.LastOrDefault()).OfType<EmailAddress>().Select((Address) => new ContactModel(Address.Address, Address.Name)).ToArray();
        }
    }
}
