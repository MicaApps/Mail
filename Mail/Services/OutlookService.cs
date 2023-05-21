using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Authentication;
using Mail.Extensions.Graph;
using Mail.Models;
using Mail.Services.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Me.MailFolders.Item;
using Microsoft.Graph.Me.MailFolders.Item.Messages;

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

        private MailFolderItemRequestBuilder GetDefatultMailFolderBuilder(MailFolderType Type)
        {
            string FolderString = Type switch
            {
                MailFolderType.Inbox => "inbox",
                MailFolderType.Drafts => "drafts",
                MailFolderType.SentItems => "sentitems",
                MailFolderType.Deleted => "deleteditems",
                MailFolderType.Junk => "junkemail",
                MailFolderType.Archive => "archive",
                _ => throw new NotSupportedException()
            };
            return Provider.GetClient().Me.MailFolders[FolderString];
        }

        public override async IAsyncEnumerable<MailFolderData> GetMailFoldersAsync(
            [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            var client = Provider.GetClient();

            var folders = (await client.Me.MailFolders.GetAsync(requestOptions => requestOptions.QueryParameters.IncludeHiddenFolders = "true" , CancelToken)).Value;
            //TODO: Remove this code when Graph beta API which include wellknown-name property become stable.
            var inbox = await client.Me.MailFolders["inbox"].GetAsync(default, CancelToken);
            var archive = await client.Me.MailFolders["archive"].GetAsync(default, CancelToken);
            var drafts = await client.Me.MailFolders["drafts"].GetAsync(default, CancelToken);
            var deleted = await client.Me.MailFolders["deleteditems"].GetAsync(default, CancelToken);
            var junkEmail = await client.Me.MailFolders["junkemail"].GetAsync(default, CancelToken);
            var sentItems = await client.Me.MailFolders["sentitems"].GetAsync(default, CancelToken);
            var syncIssues = await client.Me.MailFolders["syncissues"].GetAsync(default, CancelToken);
            var hasDeleted = false;
            foreach (var folder in folders)
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



        public override async Task<MailFolderDetailData> GetMailFolderDetailAsync(MailFolderType Type,
            CancellationToken CancelToken = default)
        {
            return await GetMailFolderDetailAsync((await GetDefatultMailFolderBuilder(Type).GetAsync()).Id, CancelToken);
        }

        public override async Task<MailFolderDetailData> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default)
        {
            var client = Provider.GetClient();
            async IAsyncEnumerable<MailFolderDetailData> GenerateSubMailFolderDataBuilder(
                MailFolderItemRequestBuilder Builder, [EnumeratorCancellation] CancellationToken CancelToken = default)
            {
                foreach (MailFolder SubFolder in (await Builder.ChildFolders.GetAsync(default, CancelToken)).Value)
                {
                    CancelToken.ThrowIfCancellationRequested();
                    var SubFolderBuilder = client.Me.MailFolders[SubFolder.Id];
                    yield return new MailFolderDetailData(SubFolder.Id, Convert.ToUInt32(SubFolder.TotalItemCount),
                        await GenerateSubMailFolderDataBuilder(SubFolderBuilder, CancelToken).ToArrayAsync());
                }
            }

            MailFolderItemRequestBuilder Builder = client.Me.MailFolders[RootFolderId];
            MailFolder MailFolder = await Builder.GetAsync(default, CancelToken);
            return new MailFolderDetailData(MailFolder.Id, Convert.ToUInt32(MailFolder.TotalItemCount),
                await GenerateSubMailFolderDataBuilder(Builder, CancelToken).ToArrayAsync(CancelToken));
        }

        public override async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(MailFolderType Type,
            uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {

            await foreach (MailMessageData Data in GetMailMessageAsync(
                               (await GetDefatultMailFolderBuilder(Type).GetAsync(default, CancelToken)).Id, StartIndex, Count, CancelToken))
            {
                yield return Data;
            }
        }

        public override async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId,
            uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            MessagesRequestBuilder Builder =
                Provider.GetClient().Me.MailFolders[RootFolderId].Messages;

            foreach (Message Message in (await Builder
                         .GetAsync(requestOptions =>
                         {
                             requestOptions.QueryParameters.Skip = (int) StartIndex;
                             requestOptions.QueryParameters.Top = (int) Count;
                         },CancelToken)).Value)
            {
                CancelToken.ThrowIfCancellationRequested();

                // the var may be null
                var MessageSender = Message.Sender;
                yield return new MailMessageData(Message.Subject,
                    Message.Id,
                    Message.SentDateTime,
                    new MailMessageRecipientData(MessageSender?.EmailAddress.Name ?? "",
                        MessageSender?.EmailAddress.Address ?? ""),
                    Message.ToRecipients.Select((Recipient) =>
                        new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                    Message.CcRecipients.Select((Recipient) =>
                        new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                    Message.BccRecipients.Select((Recipient) =>
                        new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                    new MailMessageContentData(Message.Body.Content, Message.BodyPreview,
                        (MailMessageContentType)Message.Body.ContentType),
                    Message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name,
                        Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size),
                        Attachment.LastModifiedDateTime.GetValueOrDefault())) ??
                    Enumerable.Empty<MailMessageAttachmentData>());
            }
        }

        public override async Task<byte[]?> GetMailMessageFileAttachmentContent(string messageId, string attachmentId)
        {
            var attachments = await GetMailMessageAttachmentsAsync(messageId);

            var attachmentItem =
                attachments.Attachments.FirstOrDefault(item =>
                    item is FileAttachment file && file.ContentId == attachmentId);

            if (attachmentItem == null)
            {
                return null;
            }

            var attachment = await Provider.GetClient().Me.Messages[messageId].Attachments[attachmentItem.Id]
                .GetAsync();

            if (attachment is FileAttachment fileAttachment)
            {
                return fileAttachment.ContentBytes;
            }

            return null;
        }

        private static readonly SemaphoreSlim GetMailMessageAttachmentsLock = new(1);

        private async Task<Message> GetMailMessageAttachmentsAsync(string messageId)
        {
            var ConcurrentReading = MemoryCache.Get<Message>(messageId);
            if (ConcurrentReading != null) return ConcurrentReading;

            await GetMailMessageAttachmentsLock.WaitAsync();
            try
            {
                var Msg = MemoryCache.Get<Message>(messageId);
                if (Msg != null) return Msg;

                var Message = await Provider.GetClient().Me.Messages[messageId]
                    .GetAsync(requestOptions => requestOptions.QueryParameters.Expand = new string[] { "attachments" } );
                MemoryCache.Set(messageId, Message);

                return Message;
            }
            finally
            {
                GetMailMessageAttachmentsLock.Release();
            }
        }

        public override async Task<IReadOnlyList<ContactModel>> GetContactsAsync(
            CancellationToken CancelToken = default)
        {
            var Contacts =
                (await Provider.GetClient().Me.Contacts.GetAsync(default, CancelToken)).Value;
            return Contacts.Select((Contact) => Contact.EmailAddresses.LastOrDefault()).OfType<EmailAddress>()
                .Select((Address) => new ContactModel(Address.Address, Address.Name)).ToArray();
        }

        public async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId, bool focused,
            uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            string type;
            if (focused)
            {
                type = "Focused";
            }
            else
            {
                type = "Other";
            }

            var Builder =
                Provider.GetClient().Me.MailFolders[RootFolderId].Messages;
            var request = Builder.GetAsync(requestConfiguration =>
            {
                var queryParameters = requestConfiguration.QueryParameters;
                queryParameters.Filter = $"sentDateTime ge 1900-01-01T00:00:00Z and inferenceClassification eq '{type}'";
                queryParameters.Orderby = new string[] { "sentDateTime desc" };
                queryParameters.Skip = (int)StartIndex;
                queryParameters.Top = (int)Count;
            }, CancelToken);
            foreach (Message Message in (await request).Value)
            {
                CancelToken.ThrowIfCancellationRequested();

                yield return new MailMessageData(Message.Subject,
                    Message.Id,
                    Message.SentDateTime,
                    new MailMessageRecipientData(Message.Sender.EmailAddress.Name, Message.Sender.EmailAddress.Address),
                    Message.ToRecipients.Select((Recipient) =>
                        new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                    Message.CcRecipients.Select((Recipient) =>
                        new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                    Message.BccRecipients.Select((Recipient) =>
                        new MailMessageRecipientData(Recipient.EmailAddress.Address, Recipient.EmailAddress.Name)),
                    new MailMessageContentData(Message.Body.Content, Message.BodyPreview,
                        (MailMessageContentType)Message.Body.ContentType),
                    Message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name,
                        Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size),
                        Attachment.LastModifiedDateTime.GetValueOrDefault())) ??
                    Enumerable.Empty<MailMessageAttachmentData>());
            }
        }

        public override async IAsyncEnumerable<MailMessageFileAttachmentData> GetMailAttachmentFileAsync(
            MailMessageListDetailViewModel currentMailModel,
            [EnumeratorCancellation] CancellationToken CancellationToken
            )
        {
            var Message = await GetMailMessageAttachmentsAsync(currentMailModel.Id);
            
            var MessageAttachments = Message.Attachments;
            foreach (var Attachment in MessageAttachments)
            {
                CancellationToken.ThrowIfCancellationRequested();
                if (Attachment is FileAttachment fileAttachment && fileAttachment.IsInline != true)
                {
                    yield return new MailMessageFileAttachmentData(fileAttachment.Name, fileAttachment.Id, fileAttachment.ContentType, (ulong)fileAttachment.ContentBytes.Length, default, fileAttachment.ContentBytes);
                }
            }
        }

        public override async Task LoadAttachmentsAndCacheAsync(string messageId, CancellationToken CancelToken = default)
        {
            var MailMessageAttachmentsAsync = await GetMailMessageAttachmentsAsync(messageId);
            foreach (var Attachment in MailMessageAttachmentsAsync.Attachments)
            {
                CancelToken.ThrowIfCancellationRequested();
                if (Attachment is not FileAttachment { ContentId: not null } Fa) continue;
                if (MemoryCache.Get(Fa.Id) is not null) continue;

                MemoryCache.Set(Fa.ContentId, new MailMessageFileAttachmentData(Fa.Name, Fa.Id, Fa.ContentType, (ulong)Fa.ContentBytes.Length, default, Fa.ContentBytes));
            }
        }
    }
}