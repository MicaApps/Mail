using CommunityToolkit.Authentication;
using Mail.Extensions.Graph;
using Mail.Models;
using Mail.Models.Enums;
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

namespace Mail.Services
{
    internal class OutlookService : OAuthMailService, IMailService.IFocusFilterSupport
    {
        private GraphServiceClient? Client;

        // dependent services
        private readonly LiteDatabaseService _liteDatabaseService;
        private readonly IMemoryCache _memoryCache;

        public OutlookService(
            LiteDatabaseService liteDatabaseService,
            IMemoryCache memoryCache) : base(WebAccountProviderType.Msa)
        {
            _liteDatabaseService = liteDatabaseService;
            _memoryCache = memoryCache;

            // TODO: 这里貌似需要添加一个监听数据库变更的玩意儿
            //var dbContext = LocalCache;
            //dbContext.OperationEvent().ExecEvent += (Entity, Type) =>
            //{
            //    if (Type is not (OperationType.Insert or OperationType.Update)) return;
            //    if (Entity is not MailFolder dbFolder) return;

            //    var collFirst = MailFoldersTree.FirstOrDefault(x => x.Id.Equals(dbFolder.Id));
            //    if (collFirst is null)
            //    {
            //        if (dbFolder.ParentFolderId.IsNullOrEmpty())
            //        {
            //            MailFoldersTree.Add(dbFolder);
            //        }

            //        return;
            //    }

            //    var collIndex = MailFoldersTree.IndexOf(collFirst);
            //    if (dbFolder.ParentFolderId.IsNullOrEmpty())
            //    {
            //        MailFoldersTree[collIndex] = dbFolder;
            //    }
            //    else
            //    {
            //        MailFoldersTree.Remove(collFirst);
            //    }
            //};
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
        public override ObservableCollection<Models.MailFolder> MailFoldersTree { get; } = new();

        async IAsyncEnumerable<MailMessage> IMailService.IFocusFilterSupport.GetMailMessageAsync(
            LoadMailMessageOption option, [EnumeratorCancellation] CancellationToken CancelToken)
        {

            if (!option.ForceReload)
            {
                foreach (var message in LocalCache.QueryMessage(option))
                {
                    _memoryCache.Set(message.Id, message);
                    yield return message;
                }
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

                if (_memoryCache.Get(message.Id) is null || option.ForceReload)
                {
                    yield return await GenAndSaveMailMessageDataAsync(rootFolderId, message, type);
                }
            }
        }

        private async Task<MailMessage> GenAndSaveMailMessageDataAsync(string RootFolderId, Message message,
            string type = "Focused")
        {
            var messageData = new MailMessage(RootFolderId, message.Subject,
                message.Id,
                message.SentDateTime,
                new MailMessageRecipient(message.Sender.EmailAddress.Name, message.Sender.EmailAddress.Address),
                message.ToRecipients.Select((Recipient) =>
                    new MailMessageRecipient(Recipient.EmailAddress.Name, Recipient.EmailAddress.Address)),
                message.CcRecipients.Select((Recipient) =>
                    new MailMessageRecipient(Recipient.EmailAddress.Name, Recipient.EmailAddress.Address)),
                message.BccRecipients.Select((Recipient) =>
                    new MailMessageRecipient(Recipient.EmailAddress.Name, Recipient.EmailAddress.Address)),
                new MailMessageContent(message.Body.Content, message.BodyPreview,
                    (MailMessageContentType)message.Body.ContentType),
                message.Attachments?.Select((Attachment) => new MailMessageAttachment(Attachment.Name,
                    Attachment.Id, Attachment.ContentType, Convert.ToUInt64(Attachment.Size),
                    Attachment.LastModifiedDateTime.GetValueOrDefault())) ??
                Enumerable.Empty<MailMessageAttachment>(), type);

            // 为了插入映射的接受者类型, 这里手动处理中间数据插入
            LocalCache.SaveMessage(messageData);

            return messageData;
        }

        private GraphServiceClient GetClient() => Client ??= Provider.GetClient();

        private async Task DefaultFolderTaskAsync(string name,
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

                await SaveSuperMailFolderData(mailFolder, type, CancellationToken);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"{nameof(DefaultFolderTaskAsync)}: {e}");
            }
        }

        public override async IAsyncEnumerable<Models.MailFolder> GetMailSuperFoldersAsync(
            [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            var folderData = _liteDatabaseService.MailFolders
                .Query()
                .Where((x) => x.Type == MailFolderType.Inbox)
                .Where((x) => x.MailType == MailType.Outlook)
                .FirstOrDefault();

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

            var treeAsync = LocalCache.QueryFolderByTree(null, MailType);

            foreach (var mailFolderData in treeAsync)
            {
                mailFolderData.RecurseLoadChildFolders(_liteDatabaseService.MailFolders);

                MailFoldersTree.Add(mailFolderData);
                yield return mailFolderData;
            }

            await LoadSuperMailFolderList(CancelToken);
        }

        private async Task LoadSuperMailFolderList(CancellationToken CancelToken)
        {
            var folders = await GetClient().Me.MailFolders.GetAsync(config =>
                {
                    const string n = nameof(Microsoft.Graph.Models.MailFolder.DisplayName);
                    config.QueryParameters.Filter = $"{n} ne 'outbox'";
                },
                cancellationToken: CancelToken);

            foreach (var mailFolder in folders.Value)
            {
                await SaveSuperMailFolderData(mailFolder, MailFolderType.Other, CancelToken);
            }
        }

        private async Task SaveSuperMailFolderData(Microsoft.Graph.Models.MailFolder mailFolder, MailFolderType MailFolderType,
            CancellationToken CancelToken = default)
        {
            var folderData = new Models.MailFolder(
                mailFolder.Id,
                mailFolder.DisplayName,
                MailFolderType,
                mailFolder.TotalItemCount ?? 0,
                MailType.Outlook,
                new List<Models.MailFolder>(0))
            {
                
            };

            LocalCache.SaveFolder(folderData);

            if ((mailFolder.ChildFolderCount ?? 0) > 0)
            {
                await LoadMailChildFolderAsync(folderData, CancelToken);
            }
        }

        private async Task LoadMailChildFolderAsync(Models.MailFolder parentMailFolder,
            CancellationToken cancelToken = default)
        {
            var folders = await GetClient().Me.MailFolders[parentMailFolder.Id].ChildFolders
                .GetAsync(cancellationToken: cancelToken);

            foreach (var mailFolder in folders.Value)
            {
                var childMailFolder = new Models.MailFolder(mailFolder.Id,
                    mailFolder.DisplayName,
                    MailFolderType.Other, 
                    mailFolder.TotalItemCount ?? 0, 
                    MailType.Outlook,
                    new List<Models.MailFolder>(0))
                {
                    ParentFolderId = parentMailFolder.Id,
                };

                if (parentMailFolder.ChildFolders == null)
                    parentMailFolder.ChildFolders = new();

                parentMailFolder.ChildFolders.Add(childMailFolder);

                LocalCache.SaveFolder(childMailFolder);

                if ((mailFolder.ChildFolderCount ?? 0) > 0) 
                    await LoadMailChildFolderAsync(childMailFolder, cancelToken);
            }
        }

        public override async Task<Models.MailFolder> GetMailFolderDetailAsync(string rootFolderId,
            CancellationToken CancelToken = default)
        {
            var rootFolder = _liteDatabaseService.MailFolders
                .Query()
                .Where(f => f.Id == rootFolderId)
                .FirstOrDefault();

            if (rootFolder != null)
                rootFolder.RecurseLoadChildFolders(_liteDatabaseService.MailFolders);

            return rootFolder;
        }

        public override async IAsyncEnumerable<MailMessage> GetMailMessageAsync(LoadMailMessageOption option,
            [EnumeratorCancellation] CancellationToken CancelToken = default)
        {
            foreach (var message in LocalCache.QueryMessage(option))
            {
                _memoryCache.Set(message.Id, message);
                yield return message;
            }

            var rootFolderId = option.FolderId;
            var builder = GetClient().Me.MailFolders[rootFolderId].Messages;

            foreach (var message in (await builder
                         .GetAsync(requestOptions =>
                         {
                             requestOptions.QueryParameters.Skip = option.StartIndex;
                             requestOptions.QueryParameters.Top = option.LoadCount;
                         }, CancelToken)).Value)
            {
                CancelToken.ThrowIfCancellationRequested();
                if (_memoryCache.Get(message.Id) is not null) continue;
                var focused = option.IsFocusedTab ? "Focused" : "Other";
                var messageData = await GenAndSaveMailMessageDataAsync(option.FolderId, message, focused);

                yield return messageData;
            }
        }

        public override async Task<byte[]?> GetMailMessageFileAttachmentContent(string messageId, string attachmentId)
        {
            var attachments = await GetMailMessageAttachmentsAsync(messageId);

            if (attachments.OfType<FileAttachment>().FirstOrDefault(file => file.ContentId == attachmentId) is
                FileAttachment attachmentItem)
            {
                var attachment = await IProviderExtension.GetClient(Provider).Me.Messages[messageId]
                    .Attachments[attachmentItem.Id]
                    .GetAsync();

                if (attachment is FileAttachment fileAttachment)
                {
                    return fileAttachment.ContentBytes;
                }
            }

            return null;
        }

        private async Task<IReadOnlyList<Attachment>> GetMailMessageAttachmentsAsync(string messageId,
            CancellationToken cancellationToken = default)
        {
            var message = await IProviderExtension.GetClient(Provider).Me.Messages[messageId]
                .Attachments
                .GetAsync(Option =>
                {
                    Option.QueryParameters.Select = new[]
                    {
                        "Id",
                        "Name",
                        "Size",
                        "lastModifiedDateTime",
                        "isInline",
                        "contentType"
                    };
                }, cancellationToken);
            return message.Value;
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
                req.URI = new Uri(req.URI + "/" + Contact.Id + "/photo/48x48/$value");
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

        public override async IAsyncEnumerable<MailMessageFileAttachment> GetMailAttachmentFileAsync(
            MailMessageListDetailViewModel currentMailModel,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<Attachment> Attachments =
                await GetMailMessageAttachmentsAsync(currentMailModel.Id, cancellationToken);

            foreach (FileAttachment Attachment in Attachments
                         .Where((Attachment) => !Attachment.IsInline.GetValueOrDefault()).OfType<FileAttachment>())
            {
                yield return new MailMessageFileAttachment(Attachment.Name, Attachment.Id,
                    Attachment.ContentType, (ulong)Attachment.Size, Attachment.LastModifiedDateTime ?? default,
                    Attachment.ContentBytes);
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
                Message = message,
                SaveToSentItems = true
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
            {
                Comment = ForwardContent,
                ToRecipients = message.ToRecipients
            });

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

        public override async Task<MailMessageFileAttachment?> UploadAttachmentAsync(
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
            return new MailMessageFileAttachment(result.Name, result.Id, result.ContentType,
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