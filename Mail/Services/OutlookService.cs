
using CommunityToolkit.Authentication;
using CommunityToolkit.Graph.Extensions;
using Mail.Services.Data;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Mail.Services
{
    internal class OutlookService : OAuthMailService, IMailService.IFocusFilterSupport
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

        public override async IAsyncEnumerable<MailFolderData> GetMailFoldersAsync([EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            var client = Provider.GetClient();

            var folders = await client.Me.MailFolders.Request().GetAsync(CancelToken);
            //TODO: Remove this code when Graph beta API which include wellknown-name property become stable.
            var inbox = await client.Me.MailFolders["inbox"].Request().GetAsync(CancelToken);
            var archive = await client.Me.MailFolders["archive"].Request().GetAsync(CancelToken);
            var drafts = await client.Me.MailFolders["drafts"].Request().GetAsync(CancelToken);
            var deleted = await client.Me.MailFolders["deleteditems"].Request().GetAsync(CancelToken);
            var junkEmail = await client.Me.MailFolders["junkemail"].Request().GetAsync(CancelToken);
            var sentItems = await client.Me.MailFolders["sentitems"].Request().GetAsync(CancelToken);
            var syncIssues = await client.Me.MailFolders["syncissues"].Request().GetAsync(CancelToken);
            folders.Remove(folders.First(item => item.Id == syncIssues.Id));
            var hasDeleted = false;
            foreach (var folder in folders.CurrentPage)
            {
                if (folder.Id == inbox.Id)
                {
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.Inbox);
                }
                else if (folder.Id == archive.Id)
                {
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.Archive);
                }
                else if (folder.Id == deleted.Id)
                {
                    hasDeleted = true;
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.Deleted);
                }
                else if (folder.Id == sentItems.Id)
                {
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.SentItems);
                }
                else if (folder.Id == junkEmail.Id)
                {
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.Junk);
                }
                else if (folder.Id == drafts.Id)
                {
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.Drafts);
                } 
                else if (folder.Id == syncIssues.Id) 
                {
                    // Skip add SyncIssues folder.
                }
                else
                {
                    yield return new MailFolderData(folder.Id, folder.DisplayName, MailFolderType.Other);
                }
                
            }
            if (!hasDeleted)
            {
                yield return new MailFolderData(deleted.Id, deleted.DisplayName, MailFolderType.Deleted);
            }
        }

        public override async Task<MailFolderDetailData> GetMailFolderDetailAsync(MailFolderType Type, CancellationToken CancelToken = default)
        {
            IMailFolderRequestBuilder Builder = Type switch
            {
                MailFolderType.Inbox => Provider.GetClient().Me.MailFolders.Inbox,
                MailFolderType.Drafts => Provider.GetClient().Me.MailFolders.Drafts,
                MailFolderType.SentItems => Provider.GetClient().Me.MailFolders.SentItems,
                MailFolderType.Deleted => Provider.GetClient().Me.MailFolders.DeletedItems,
                _ => throw new NotSupportedException()
            };

            return await GetMailFolderDetailAsync((await Builder.Request().GetAsync()).Id, CancelToken);
        }

        public override async Task<MailFolderDetailData> GetMailFolderDetailAsync(string RootFolderId, CancellationToken CancelToken = default)
        {
            async IAsyncEnumerable<MailFolderDetailData> GenerateSubMailFolderDataBuilder(IMailFolderRequestBuilder Builder, [EnumeratorCancellation] CancellationToken CancelToken = default)
            {
                foreach (MailFolder SubFolder in await Builder.ChildFolders.Request().GetAsync(CancelToken))
                {
                    CancelToken.ThrowIfCancellationRequested();
                    IMailFolderRequestBuilder SubFolderBuilder = Builder.ChildFolders[SubFolder.Id];
                    yield return new MailFolderDetailData(SubFolder.Id, Convert.ToUInt32(SubFolder.TotalItemCount), await GenerateSubMailFolderDataBuilder(SubFolderBuilder, CancelToken).ToArrayAsync());
                }
            }

            IMailFolderRequestBuilder Builder = Provider.GetClient().Me.MailFolders[RootFolderId];
            MailFolder MailFolder = await Builder.Request().GetAsync(CancelToken);
            return new MailFolderDetailData(MailFolder.Id, Convert.ToUInt32(MailFolder.TotalItemCount), await GenerateSubMailFolderDataBuilder(Builder, CancelToken).ToArrayAsync(CancelToken));
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
                                                 Message.Id,
                                                 Message.SentDateTime,
                                                 new MailMessageRecipientData(Message.Sender.EmailAddress.Name, Message.Sender.EmailAddress.Address),
                                                 Message.ToRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 Message.CcRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 Message.BccRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 new MailMessageContentData(Message.Body.Content, Message.BodyPreview, (MailMessageContentType)Message.Body.ContentType),
                                                 Message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name, Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size), Attachment.LastModifiedDateTime.GetValueOrDefault())) ?? Enumerable.Empty<MailMessageAttachmentData>());
            }
        }

        public override async Task<byte[]> GetMailMessageFileAttachmentContent(string messageId, string attachmentId)
        {
            var attachments = await Provider.GetClient().Me.Messages[messageId]
                .Request()
                .Expand("attachments")
                .GetAsync();

            var attachmentItem = attachments.Attachments.FirstOrDefault(item => item is FileAttachment file && file.ContentId == attachmentId);

            if (attachmentItem == null)
            {
                return null;
            }
            
            var attachment = await Provider.GetClient().Me.Messages[messageId].Attachments[attachmentItem.Id]
                .Request()
                .GetAsync();
            
            if (attachment is FileAttachment fileAttachment)
            {
                return fileAttachment.ContentBytes;
            }
            return null;
        }

        public override async Task<IReadOnlyList<ContactModel>> GetContactsAsync(CancellationToken CancelToken = default)
        {
            IUserContactsCollectionPage Contacts = await Provider.GetClient().Me.Contacts.Request().GetAsync(CancelToken);
            return Contacts.Select((Contact) => Contact.EmailAddresses.LastOrDefault()).OfType<EmailAddress>().Select((Address) => new ContactModel(Address.Address, Address.Name)).ToArray();
        }

        public async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, bool focused, uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            string type;
            if (focused)
            {
                type = "Focused";
            } else
            {
                type = "Other";
            }
            IMailFolderMessagesCollectionRequestBuilder Builder = Provider.GetClient().Me.MailFolders[RootFolderId].Messages;
            var request = Builder.Request()
                .Filter($"sentDateTime ge 1900-01-01T00:00:00Z and inferenceClassification eq '{type}'")
                .OrderBy("sentDateTime desc")
                .Skip((int)StartIndex)
                .Top((int)Count)
                .GetAsync(CancelToken);
            foreach (Message Message in await request)
            {
                CancelToken.ThrowIfCancellationRequested();

                yield return new MailMessageData(Message.Subject,
                                                 Message.Id,
                                                 Message.SentDateTime,
                                                 new MailMessageRecipientData(Message.Sender.EmailAddress.Name, Message.Sender.EmailAddress.Address),
                                                 Message.ToRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 Message.CcRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 Message.BccRecipients.Select((Recipient) => new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                                                 new MailMessageContentData(Message.Body.Content, Message.BodyPreview, (MailMessageContentType)Message.Body.ContentType),
                                                 Message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name, Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size), Attachment.LastModifiedDateTime.GetValueOrDefault())) ?? Enumerable.Empty<MailMessageAttachmentData>());
            }
        }
    }
}
