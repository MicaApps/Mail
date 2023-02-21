
using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using Mail.Class.Data;
using Mail.Class.Models;
using Mail.Enum;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mail.Servives
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

        public async Task<MailFolderData> GetMailFolderAsync(MailFolderType Type, int StartIndex, int Count)
        {
            async IAsyncEnumerable<MailFolderData> GenerateSubMailFolderDataBuilder(IMailFolderRequestBuilder Builder, int StartIndex, int Count)
            {
                foreach (MailFolder SubFolder in await Builder.ChildFolders.Request().Skip(StartIndex).Top(Count).GetAsync())
                {
                    yield return new MailFolderData(GenerateSubMailFolderData(SubFolder), GenerateMailMessageData(SubFolder.Messages));
                }
            }

            async IAsyncEnumerable<MailMessageData> GenerateMailMessageDataBuilder(IMailFolderMessagesCollectionRequestBuilder Builder, int StartIndex, int Count)
            {
                foreach (MailMessageData Data in GenerateMailMessageData(await Builder.Request().Skip(StartIndex).Top(Count).GetAsync()))
                {
                    yield return Data;
                }
            }

            IEnumerable<MailFolderData> GenerateSubMailFolderData(MailFolder Folder)
            {
                foreach (MailFolder SubFolder in Folder.ChildFolders)
                {
                    yield return new MailFolderData(GenerateSubMailFolderData(Folder), GenerateMailMessageData(Folder.Messages));
                }
            }

            IEnumerable<MailMessageData> GenerateMailMessageData(IEnumerable<Message> MessageCollection)
            {
                return MessageCollection.Select((Message) => new MailMessageData(Message.Subject,
                                                                                 new MailMessageRecipientData(Message.Sender.EmailAddress.Name, Message.Sender.EmailAddress.Address),
                                                                                 Message.ToRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                                                 Message.CcRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                                                 Message.BccRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                                                 new MailMessageContentData(Message.Body.Content, Message.BodyPreview, (MailMessageContentType)Message.Body.ContentType),
                                                                                 Message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name, Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size), Attachment.LastModifiedDateTime.GetValueOrDefault())) ?? Enumerable.Empty<MailMessageAttachmentData>()));
            }

            IMailFolderRequestBuilder Builder = Type switch
            {
                MailFolderType.Inbox => Provider.GetClient().Me.MailFolders.Inbox,
                MailFolderType.Drafts => Provider.GetClient().Me.MailFolders.Drafts,
                MailFolderType.SentItems => Provider.GetClient().Me.MailFolders.SentItems,
                MailFolderType.Deleted => Provider.GetClient().Me.MailFolders.DeletedItems,
                _ => throw new NotSupportedException()
            };

            return new MailFolderData(await GenerateSubMailFolderDataBuilder(Builder, StartIndex, Count).ToArrayAsync(), await GenerateMailMessageDataBuilder(Builder.Messages, StartIndex, Count).ToArrayAsync());
        }

        public async Task<IReadOnlyList<ContactModel>> GetContactsAsync()
        {
            IUserContactsCollectionPage Contacts = await Provider.GetClient().Me.Contacts.Request().GetAsync();
            return Contacts.Select((Contact) => Contact.EmailAddresses.LastOrDefault()).OfType<EmailAddress>().Select((Address) => new ContactModel(Address.Address, Address.Name)).ToArray();
        }
    }
}
