using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using CommunityToolkit.Authentication;
using Mail.Extensions;
using Mail.Extensions.Graph;
using Mail.Models;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Me.Messages.Item.Attachments.CreateUploadSession;
using Microsoft.Graph.Me.Messages.Item.Forward;
using Microsoft.Graph.Me.Messages.Item.Move;
using Microsoft.Graph.Me.Messages.Item.Reply;
using Microsoft.Graph.Me.Messages.Item.ReplyAll;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using SqlSugar;

namespace Mail.Services
{
    internal class OutlookService : OAuthMailService, IMailService.IFocusFilterSupport
    {
        private static readonly SemaphoreSlim GetMailMessageAttachmentsLock = new(1);
        private GraphServiceClient? Client;

        public OutlookService() : base(WebAccountProviderType.Msa)
        {
            DbClient.GetDbOperationEvent().ExecEvent += (Entity, Type) =>
            {
                if (Type != DataFilterType.InsertByObject) return;
                if (Entity is not MailFolderData { ParentFolderId: "" } f) return;
                MailFoldersTree.Add(f);
            };
        }

        protected override string[] Scopes { get; } =
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

        public override MailType MailType => MailType.Outlook;
        public override ObservableCollection<MailFolderData> MailFoldersTree { get; } = new();

        async IAsyncEnumerable<MailMessageData> IMailService.IFocusFilterSupport.GetMailMessageAsync(
            LoadMailMessageOption option, [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            foreach (var message in GetCacheMessageData(option))
            {
                MemoryCache.Set(message.Id, message);
                yield return message;
            }

            var type = option.IsFocusedTab ? "Focused" : "Other";
            var rootFolderId = option.FolderId;
            var builder = GetClient().Me.MailFolders[rootFolderId].Messages;
            var enumerable = (await builder.GetAsync(requestConfiguration =>
            {
                var queryParameters = requestConfiguration.QueryParameters;
                queryParameters.Filter =
                    $"sentDateTime ge 1900-01-01T00:00:00Z and inferenceClassification eq '{type}'";
                queryParameters.Orderby = new[] { "sentDateTime desc" };
                queryParameters.Skip = option.StartIndex;
                queryParameters.Top = option.LoadCount;
            }, CancelToken)).Value;

            foreach (var message in enumerable)
            {
                CancelToken.ThrowIfCancellationRequested();
                if (MemoryCache.Get(message.Id) is not null)
                {
                    continue;
                }

                var messageData = GenAndSaveMailMessageData(rootFolderId, message, type);

                yield return messageData;
            }
        }

        private IEnumerable<MailMessageData> GetCacheMessageData(LoadMailMessageOption Option)
        {
            var focused = Option.IsFocusedTab ? "Focused" : "Other";
            var messageList = DbClient.Queryable<MailMessageData>()
                .Includes(x => x.Content)
                .Mapper((x, cache) =>
                {
                    x.Sender = DbClient.Queryable<MailMessageRecipientData>()
                        .Where(recipient => recipient.Id.Equals(x.Id))
                        .Where(recipient => recipient.RecipientType == RecipientType.Sender)
                        .First();
                })
                .Includes(x => x.To.Where(recipient => recipient.RecipientType == RecipientType.To).ToList())
                .Includes(x => x.CC.Where(recipient => recipient.RecipientType == RecipientType.Cc).ToList())
                .Includes(x => x.Bcc.Where(recipient => recipient.RecipientType == RecipientType.Bcc).ToList())
                .Where(x => x.FolderId.Equals(Option.FolderId))
                .Where(x => x.InferenceClassification.Equals(focused))
                .Skip(Option.StartIndex)
                .Take(Option.LoadCount)
                .OrderByDescending(x => x.SentTime)
                .ToList();
            return messageList;
        }

        private MailMessageData GenAndSaveMailMessageData(string RootFolderId, Message message, string type = "Focused")
        {
            var messageData = new MailMessageData(RootFolderId, message.Subject,
                message.Id,
                message.SentDateTime,
                new MailMessageRecipientData(message.Sender.EmailAddress.Name, message.Sender.EmailAddress.Address),
                message.ToRecipients.Select((Recipient) =>
                    new MailMessageRecipientData(Recipient.EmailAddress.Name, Recipient.EmailAddress.Address)),
                message.CcRecipients.Select((Recipient) =>
                    new MailMessageRecipientData(Recipient.EmailAddress.Name, Recipient.EmailAddress.Address)),
                message.BccRecipients.Select((Recipient) =>
                    new MailMessageRecipientData(Recipient.EmailAddress.Name, Recipient.EmailAddress.Address)),
                new MailMessageContentData(message.Body.Content, message.BodyPreview,
                    (MailMessageContentType)message.Body.ContentType),
                message.Attachments?.Select((Attachment) => new MailMessageAttachmentData(Attachment.Name,
                    Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size),
                    Attachment.LastModifiedDateTime.GetValueOrDefault())) ??
                Enumerable.Empty<MailMessageAttachmentData>(), type);

            // 为了插入映射的接受者类型, 这里手动处理中间数据插入
            Task.Run(() =>
            {
                DbClient.SaveOrUpdate(messageData.GetRecipientData());
                DbClient.InsertNav(messageData)
                    .Include(x => x.Content)
                    .ExecuteCommandAsync();
            });
            return messageData;
        }

        private GraphServiceClient GetClient() => Client ??= Provider.GetClient();

        private async Task<MailFolderCollectionResponse?> DefaultFolderTaskAsync(string name,
            CancellationToken CancellationToken)
        {
            try
            {
                var mailFolder = await GetClient().Me.MailFolders[name].GetAsync(cancellationToken: CancellationToken);
                var type = name switch
                {
                    "inbox" => MailFolderType.Inbox,
                    "archive" => MailFolderType.Archive,
                    "deleteditems" => MailFolderType.Deleted,
                    "junkemail" => MailFolderType.Junk,
                    "sentitems" => MailFolderType.SentItems,
                    "drafts" => MailFolderType.Drafts,
                    _ => MailFolderType.Other
                };
                SaveSuperMailFolderData(mailFolder, type, CancellationToken);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }

            return null;
        }

        public override async IAsyncEnumerable<MailFolderData> GetMailSuperFoldersAsync(
            [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            var folderData = await DbClient.Queryable<MailFolderData>().Where(x => x.Type == MailFolderType.Inbox)
                .Where(x => x.MailType == MailType.Outlook).FirstAsync(CancelToken);
            if (folderData == null)
            {
                await Task.WhenAll(DefaultFolderTaskAsync("inbox", CancelToken),
                    DefaultFolderTaskAsync("archive", CancelToken),
                    DefaultFolderTaskAsync("deleteditems", CancelToken),
                    DefaultFolderTaskAsync("junkemail", CancelToken),
                    DefaultFolderTaskAsync("sentitems", CancelToken),
                    DefaultFolderTaskAsync("syncissues", CancelToken),
                    DefaultFolderTaskAsync("drafts", CancelToken));
            }

            var treeAsync = await DbClient.Queryable<MailFolderData>()
                .Where(x => x.MailType == MailType)
                .ToTreeAsync(x => x.ChildFolders, x => x.ParentFolderId, "");

            foreach (var mailFolderData in treeAsync)
            {
                mailFolderData.RecursionChildFolderToObservableCollection(DbClient);

                MailFoldersTree.Add(mailFolderData);
                yield return mailFolderData;
            }

            LoadSuperMailFolderList(CancelToken);
        }

        private async Task LoadSuperMailFolderList(CancellationToken CancelToken)
        {
            var folders = await GetClient().Me.MailFolders.GetAsync(config =>
                {
                    const string n = nameof(MailFolder.DisplayName);
                    config.QueryParameters.Filter = $"{n} ne 'outbox'";
                },
                cancellationToken: CancelToken);

            foreach (var mailFolder in folders.Value)
            {
                SaveSuperMailFolderData(mailFolder, MailFolderType.Other, CancelToken);
            }
        }

        private async Task SaveSuperMailFolderData(MailFolder mailFolder, MailFolderType MailFolderType,
            CancellationToken CancelToken = default)
        {
            var folderData = new MailFolderData(mailFolder.Id,
                mailFolder.DisplayName,
                MailFolderType,
                new List<MailFolderData>(0),
                mailFolder.TotalItemCount,
                MailType.Outlook)
            {
                ChildFolderCount = mailFolder.ChildFolderCount ?? 0,
            };
            folderData.RecursionChildFolderToObservableCollection(DbClient);

            var dbD = await DbClient.Queryable<MailFolderData>().Where(x => x.Id.Equals(mailFolder.Id))
                .FirstAsync(CancelToken);
            if (dbD is not null && dbD.Type != MailFolderType.Other)
            {
                folderData.Type = dbD.Type;
            }

            await DbClient.SaveOrUpdate(folderData, CancellationToken: CancelToken);

            if (folderData.ChildFolderCount > 0) LoadMailChildFolderAsync(folderData, CancelToken);
        }

        private async Task LoadMailChildFolderAsync(MailFolderData ParentMailFolder,
            CancellationToken CancelToken = default)
        {
            var folders = await GetClient().Me.MailFolders[ParentMailFolder.Id].ChildFolders
                .GetAsync(cancellationToken: CancelToken);

            foreach (var mailFolder in folders.Value)
            {
                var childMailFolder = new MailFolderData(mailFolder.Id,
                    mailFolder.DisplayName,
                    MailFolderType.Other,
                    new List<MailFolderData>(0), mailFolder.TotalItemCount, MailType.Outlook)
                {
                    ParentFolderId = ParentMailFolder.Id,
                    ChildFolderCount = mailFolder.ChildFolderCount ?? 0
                };
                childMailFolder.RecursionChildFolderToObservableCollection(DbClient);

                await DbClient.SaveOrUpdate(childMailFolder, CancelToken);

                if (childMailFolder.ChildFolderCount > 0)
                {
                    await LoadMailChildFolderAsync(childMailFolder, CancelToken);
                }
            }
        }

        public override async Task<MailFolderData> GetMailFolderDetailAsync(string RootFolderId,
            CancellationToken CancelToken = default)
        {
            var treeAsync = await DbClient.Queryable<MailFolderData>()
                .Where(x => x.MailType == MailType)
                .ToTreeAsync(x => x.ChildFolders, x => x.ParentFolderId, RootFolderId);
            var rootFolder = await DbClient.Queryable<MailFolderData>().Where(x => x.Id.Equals(RootFolderId))
                .FirstAsync(CancelToken);
            rootFolder.ChildFolders = treeAsync;

            return rootFolder;
        }

        public override async IAsyncEnumerable<MailMessageData> GetMailMessageAsync(LoadMailMessageOption option,
            [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            foreach (var message in GetCacheMessageData(option))
            {
                MemoryCache.Set(message.Id, message);
                yield return message;
            }

            var rootFolderId = option.FolderId;
            var builder = GetClient().Me.MailFolders[rootFolderId].Messages;

            foreach (Message message in (await builder
                         .GetAsync(requestOptions =>
                         {
                             requestOptions.QueryParameters.Skip = option.StartIndex;
                             requestOptions.QueryParameters.Top = option.LoadCount;
                         }, CancelToken)).Value)
            {
                CancelToken.ThrowIfCancellationRequested();
                if (MemoryCache.Get(message.Id) is not null)
                {
                    continue;
                }

                var messageData = GenAndSaveMailMessageData(option.FolderId, message);

                yield return messageData;
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

        public override async Task UploadAttachmentSessionAsync(MailMessageListDetailViewModel Model,
            BasicProperties BasicProperties, StorageFile StorageFile,
            Action<long> UploadedSliceCallback,
            CancellationToken CancelToken = default)
        {
            if (Model.Id.IsNullOrEmpty())
            {
                return;
            }

            var builder = IProviderExtension.GetClient(Provider).Me.Messages[Model.Id].Attachments.CreateUploadSession;
            var uploadSession = await builder.PostAsync(new CreateUploadSessionPostRequestBody
            {
                AttachmentItem = new AttachmentItem
                {
                    AttachmentType = AttachmentType.File,
                    Name = StorageFile.Name,
                    Size = (long)BasicProperties.Size
                }
            }, cancellationToken: CancelToken);

            const int maxSliceSize = 320 * 1024 * 2;
            var fileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession,
                (await StorageFile.OpenSequentialReadAsync()).AsStreamForRead(),
                maxSliceSize);
            var totalLength = BasicProperties.Size;
            // Create a callback that is invoked after each slice is uploaded
            IProgress<long> callback = new Progress<long>(UploadedSliceCallback);

            try
            {
                var uploadResult = await fileUploadTask.UploadAsync(callback, cancellationToken: CancelToken);
                Trace.WriteLine(uploadResult.UploadSucceeded
                    ? $"Upload complete, item ID: {uploadResult.ItemResponse?.Id}"
                    : "Upload failed");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        public override async Task<bool> MailMoveAsync(string mailMessageId, string folderId)
        {
            var postAsync = await IProviderExtension.GetClient(Provider).Me.Messages[mailMessageId].Move.PostAsync(
                new MovePostRequestBody
                {
                    DestinationId = folderId
                });

            return postAsync != null;
        }

        public override async Task<MailMessageFileAttachmentData?> UploadAttachmentAsync(
            MailMessageListDetailViewModel Model,
            StorageFile StorageFile,
            CancellationToken CancelToken = default)
        {
            var arb = IProviderExtension.GetClient(Provider).Me.Messages[Model.Id].Attachments;

            var readBytesAsync = await StorageFile.ReadBytesAsync();
            var result = await arb.PostAsync(new FileAttachment
            {
                Name = StorageFile.Name,
                ContentType = StorageFile.ContentType,
                ContentBytes = readBytesAsync
            }, cancellationToken: CancelToken);
            return new MailMessageFileAttachmentData(result.Name, result.Id, result.ContentType,
                (ulong)(result.Size ?? 0), default, readBytesAsync);
        }

        public override async Task<bool> MailRemoveAsync(MailMessageListDetailViewModel Model)
        {
            if (Model.Id.IsNullOrEmpty()) return false;

            await IProviderExtension.GetClient(Provider).Me.Messages[Model.Id].DeleteAsync();
            return true;
        }
    }
}