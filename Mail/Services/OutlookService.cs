using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Authentication;
using Mail.Extensions;
using Mail.Extensions.Graph;
using Mail.Models;
using Mail.Services.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Me.MailFolders.Item;
using Microsoft.Graph.Me.MailFolders.Item.Messages;
using Microsoft.Graph.Me.Messages.Item.Forward;
using Microsoft.Graph.Me.Messages.Item.Reply;
using Microsoft.Graph.Me.Messages.Item.ReplyAll;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;

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
            "offline_access",
            "Mail.Send"
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
            return IProviderExtension.GetClient(Provider).Me.MailFolders[FolderString];
        }

        private async Task<MailFolder> CatchMailFolder(MailFolderItemRequestBuilder Builder,
            CancellationToken CancelToken = default)
        {
            return await RunCatch.GetOrDefault(Builder.GetAsync(default, CancelToken));
        }

        public override async IAsyncEnumerable<MailFolderData> GetMailFoldersAsync(
            [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            var client = IProviderExtension.GetClient(Provider);

            //TODO: Remove this code when Graph beta API which include wellknown-name property become stable.
            var inbox = await CatchMailFolder(client.Me.MailFolders["inbox"], CancelToken);
            var archive = await CatchMailFolder(client.Me.MailFolders["archive"], CancelToken);
            var drafts = await CatchMailFolder(client.Me.MailFolders["drafts"], CancelToken);
            var deleted = await CatchMailFolder(client.Me.MailFolders["deleteditems"], CancelToken);
            var junkEmail = await CatchMailFolder(client.Me.MailFolders["junkemail"], CancelToken);
            var sentItems = await CatchMailFolder(client.Me.MailFolders["sentitems"], CancelToken);
            var syncIssues = await CatchMailFolder(client.Me.MailFolders["syncissues"], CancelToken);

            MailFolderCollectionResponse folders = null;
            var count = 0;

            async IAsyncEnumerable<MailFolderData> GenerateSubMailFolderDataBuilder(
                MailFolderItemRequestBuilder Builder, [EnumeratorCancellation] CancellationToken CancelToken = default)
            {
                var subFolderCount = 0;
                MailFolderCollectionResponse subFolders = null;
                do
                {
                    subFolders = (await Builder.ChildFolders.GetAsync(
                        requestConfiguration => { requestConfiguration.QueryParameters.Skip = subFolderCount; },
                        CancelToken));
                    subFolderCount += subFolders.Value.Count;
                    foreach (MailFolder SubFolder in subFolders.Value)
                    {
                        CancelToken.ThrowIfCancellationRequested();
                        var SubFolderBuilder = client.Me.MailFolders[SubFolder.Id];
                        yield return new MailFolderData(SubFolder.Id, SubFolder.DisplayName, MailFolderType.Other,
                            await GenerateSubMailFolderDataBuilder(SubFolderBuilder, CancelToken).ToArrayAsync());
                    }
                } while (subFolders.OdataNextLink != null);
            }

            do
            {
                folders = (await client.Me.MailFolders.GetAsync(requestOptions =>
                {
                    requestOptions.QueryParameters.IncludeHiddenFolders = "true";
                    requestOptions.QueryParameters.Skip = count;
                }, CancelToken));

                foreach (var folder in folders.Value)
                {
                    MailFolderType FolderType = MailFolderType.Other;
                    if (inbox != null && folder.Id == inbox.Id)
                    {
                        FolderType = MailFolderType.Inbox;
                    }
                    else if (archive != null && folder.Id == archive.Id)
                    {
                        FolderType = MailFolderType.Archive;
                    }
                    else if (deleted != null && folder.Id == deleted.Id)
                    {
                        FolderType = MailFolderType.Deleted;
                    }
                    else if (sentItems != null && folder.Id == sentItems.Id)
                    {
                        FolderType = MailFolderType.SentItems;
                    }
                    else if (junkEmail != null && folder.Id == junkEmail.Id)
                    {
                        FolderType = MailFolderType.Junk;
                    }
                    else if (drafts != null && folder.Id == drafts.Id)
                    {
                        FolderType = MailFolderType.Drafts;
                        this.DraftFolderId = drafts.Id;
                    }
                    else if (syncIssues != null && folder.Id == syncIssues.Id)
                    {
                        continue;
                    }

                    yield return new MailFolderData(folder.Id, folder.DisplayName, FolderType,
                        await GenerateSubMailFolderDataBuilder(client.Me.MailFolders[folder.Id]).ToArrayAsync());
                }

                count += folders.Value.Count;
            } while (folders.OdataNextLink != null);
        }


        public override async Task<MailFolderDetailData> GetMailFolderDetailAsync(MailFolderType Type,
            CancellationToken CancelToken = default)
        {
            return await GetMailFolderDetailAsync((await GetDefatultMailFolderBuilder(Type).GetAsync()).Id,
                CancelToken);
        }

        public override async Task<MailFolderDetailData> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default)
        {
            var client = IProviderExtension.GetClient(Provider);

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
            await foreach (var data in GetMailMessageAsync(
                               (await GetDefatultMailFolderBuilder(Type).GetAsync(default, CancelToken)).Id, StartIndex,
                               Count, CancelToken))
            {
                yield return data;
            }
        }

        public override async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(string RootFolderId,
            uint StartIndex = 0, uint Count = 30, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            MessagesRequestBuilder Builder =
                IProviderExtension.GetClient(Provider).Me.MailFolders[RootFolderId].Messages;

            foreach (Message Message in (await Builder
                         .GetAsync(requestOptions =>
                         {
                             requestOptions.QueryParameters.Skip = (int)StartIndex;
                             requestOptions.QueryParameters.Top = (int)Count;
                         }, CancelToken)).Value)
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

            var attachment = await IProviderExtension.GetClient(Provider).Me.Messages[messageId]
                .Attachments[attachmentItem.Id]
                .GetAsync();

            if (attachment is FileAttachment fileAttachment)
            {
                return fileAttachment.ContentBytes;
            }

            return null;
        }

        private static readonly SemaphoreSlim GetMailMessageAttachmentsLock = new(1);
        private string? DraftFolderId;

        private async Task<Message> GetMailMessageAttachmentsAsync(string messageId)
        {
            var ConcurrentReading = MemoryCache.Get<Message>(messageId);
            if (ConcurrentReading != null) return ConcurrentReading;

            await GetMailMessageAttachmentsLock.WaitAsync();
            try
            {
                var Msg = MemoryCache.Get<Message>(messageId);
                if (Msg != null) return Msg;

                var Message = await IProviderExtension.GetClient(Provider).Me.Messages[messageId]
                    .GetAsync(requestOptions => requestOptions.QueryParameters.Expand = new string[] { "attachments" });
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
                (await IProviderExtension.GetClient(Provider).Me.Contacts
                    .GetAsync((option) => option.QueryParameters.Top = 1000, CancelToken)).Value;

            var batch = new BatchRequestContentCollection(IProviderExtension.GetClient(Provider));
            Dictionary<string, string> UserToIdMapping = new Dictionary<string, string>();
            foreach (var Contact in Contacts)
            {
                var req = IProviderExtension.GetClient(Provider).Me.Contacts.ToGetRequestInformation();
                req.URI = new Uri(req.URI + "/" + Contact.Id + "/photo/$value");
                var id = await batch.AddBatchRequestStepAsync(req);
                UserToIdMapping[Contact.Id] = id;
            }

            var batchBuilder = IProviderExtension.GetClient(Provider).Batch;

            var photos = await batchBuilder.PostAsync(batch, CancelToken);
            var ret = new List<ContactModel>();
            foreach (var Contact in Contacts)
            {
                ret.Add(new ContactModel(Contact.DisplayName,
                    Contact.EmailAddresses.LastOrDefault()?.Address ?? string.Empty,
                    await photos.GetResponseStreamByIdAsync(UserToIdMapping[Contact.Id])));
            }

            return ret;
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
                IProviderExtension.GetClient(Provider).Me.MailFolders[RootFolderId].Messages;
            var request = Builder.GetAsync(requestConfiguration =>
            {
                var queryParameters = requestConfiguration.QueryParameters;
                queryParameters.Filter =
                    $"sentDateTime ge 1900-01-01T00:00:00Z and inferenceClassification eq '{type}'";
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
                    yield return new MailMessageFileAttachmentData(fileAttachment.Name, fileAttachment.Id,
                        fileAttachment.ContentType, (ulong)fileAttachment.ContentBytes.Length, default,
                        fileAttachment.ContentBytes);
                }
            }
        }

        public override async Task LoadAttachmentsAndCacheAsync(string messageId,
            CancellationToken CancelToken = default)
        {
            var MailMessageAttachmentsAsync = await GetMailMessageAttachmentsAsync(messageId);
            foreach (var Attachment in MailMessageAttachmentsAsync.Attachments)
            {
                CancelToken.ThrowIfCancellationRequested();
                if (Attachment is not FileAttachment { ContentId: not null } Fa) continue;
                if (MemoryCache.Get(Fa.Id) is not null) continue;

                MemoryCache.Set(Fa.ContentId,
                    new MailMessageFileAttachmentData(Fa.Name, Fa.Id, Fa.ContentType, (ulong)Fa.ContentBytes.Length,
                        default, Fa.ContentBytes));
            }
        }

        public override async Task<bool> MailDraftSaveAsync(MailMessageListDetailViewModel Model)
        {
            var rb = IProviderExtension.GetClient(Provider).Me;
            var message = ToMessage(Model);

            if (message is null) return false;
            Message result;
            if (Model.Id.IsNullOrEmpty())
            {
                result = await rb.Messages.PostAsync(message);
                var response = await rb.Messages.GetAsync(config =>
                {
                    config.QueryParameters.Filter = "IsDraft eq true";
                    config.QueryParameters.Top = 1;
                });

                Model.Id = response.Value.FirstOrDefault()?.Id ?? "";
            }
            else
            {
                result = await rb.Messages[Model.Id].PatchAsync(message);
            }

            // TODO deserializeObject exception
            return result is not null;
        }

        public override async Task<bool> MailSendAsync(MailMessageListDetailViewModel Model)
        {
            var rb = IProviderExtension.GetClient(Provider).Me;
            var message = ToMessage(Model);
            if (message is null) return false;

            // TODO err
            await rb.SendMail.PostAsync(new SendMailPostRequestBody
            {
                Message = message, SaveToSentItems = true
            });

            return true;
        }

        private static Message? ToMessage(MailMessageListDetailViewModel Model)
        {
            var jsonSetting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            var serializeObject = JsonConvert.SerializeObject(Model);
            var message = JsonConvert.DeserializeObject<Message>(serializeObject, jsonSetting);
            return message;
        }

        public override async Task<bool> MailReplyAsync(MailMessageListDetailViewModel Model, string ReplyContent,
            bool IsAll = false)
        {
            var me = IProviderExtension.GetClient(Provider).Me;
            var message = ToMessage(Model);
            if (message is null)
            {
                return false;
            }

            if (IsAll)
            {
                await me.Messages[Model.Id].ReplyAll.PostAsync(
                    new ReplyAllPostRequestBody { Comment = ReplyContent });
            }
            else
            {
                await me.Messages[Model.Id].Reply.PostAsync(
                    new ReplyPostRequestBody { Message = message, Comment = ReplyContent });
            }

            return true;
        }

        public override async Task<bool> MailForwardAsync(MailMessageListDetailViewModel Model,
            string ForwardContent)
        {
            var me = IProviderExtension.GetClient(Provider).Me;
            var message = ToMessage(Model);
            if (message is null) return false;

            await me.Messages[Model.Id].Forward.PostAsync(new ForwardPostRequestBody
                { ToRecipients = message.ToRecipients, Comment = ForwardContent });
            return true;
        }

        public override async Task UploadAttachmentSessionAsync(StorageFile StorageFile,
            CancellationToken CancelToken = default)
        {
        }

        public override async Task<MailMessageFileAttachmentData?> UploadAttachmentAsync(
            MailMessageListDetailViewModel Model,
            StorageFile StorageFile,
            CancellationToken CancelToken = default)
        {
            var arb = Provider.GetClient().Me.Events[Model.Id].Attachments;

            var result = await arb.PostAsync(new FileAttachment()
            {
                Name = StorageFile.Name,
                ContentType = StorageFile.ContentType,
                ContentBytes = await StorageFile.ReadBytesAsync()
            });
            var json = JsonConvert.SerializeObject(result);
            return JsonConvert.DeserializeObject<MailMessageFileAttachmentData>(json);
        }

        public override async Task<bool> RemoveMailAsync(MailMessageListDetailViewModel Model)
        {
            if (Model.Id.IsNullOrEmpty()) return false;

            await Provider.GetClient().Me.Messages[Model.Id].DeleteAsync();
            return true;
        }
    }
}