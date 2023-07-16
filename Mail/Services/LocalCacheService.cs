using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Chloe;
using Mail.Events;
using Mail.Extensions;
using Mail.Models;
using Mail.Services.Data;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Mail.Services;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/07/16
/// </summary>
public class LocalCacheService
{
    public IDbContext GetContext() => App.Services.GetService<IDbContext>()!;

    public void SaveMessage(MailMessageData Data)
    {
        using var dbContext = GetContext();
        Data.SaveRecipientData(dbContext);
        dbContext.InsertOrUpdate(Data.Content);
        dbContext.InsertOrUpdate(Data);
    }

    public async Task SaveFolder(MailFolderData Data)
    {
        var db = GetContext();
        var dbFolder = await db.Query<MailFolderData>().Where(x => x.Id == Data.Id)
            .FirstOrDefaultAsync();
        if (dbFolder is not null && dbFolder.Type != MailFolderType.Other) Data.Type = dbFolder.Type;

        await db.InsertOrUpdateAsync(Data);
    }

    public List<MailMessageData> QueryMessage(LoadMailMessageOption Option)
    {
        var focused = Option.IsFocusedTab ? "Focused" : "Other";
        using var db = GetContext();
        try
        {
            var messageList = db.Query<MailMessageData>()
                .Include(x => x.Content)
                .Where(x => x.FolderId == Option.FolderId)
                .Where(x => x.InferenceClassification == focused)
                .Skip(Option.StartIndex)
                .Take(Option.LoadCount)
                .OrderByDesc(x => x.SentTime)
                .ToList();

            Parallel.ForEach(messageList, item =>
            {
                using var dbContext = GetContext();
                var sender = dbContext.Query<MailMessageRecipientData>()
                    .Where(x => x.MessageId == item.MessageId)
                    .Where(x => x.RecipientType == RecipientType.Sender)
                    .FirstOrDefault();
                if (sender is not null)
                {
                    item.Sender = sender;
                }

                {
                    var toList = dbContext.Query<MailMessageRecipientData>()
                        .Where(x => x.MessageId == item.MessageId)
                        .Where(x => x.RecipientType == RecipientType.To)
                        .ToList();
                    if (toList.IsNullOrEmpty())
                    {
                        item.To = toList;
                    }
                }
                {
                    var toList = dbContext.Query<MailMessageRecipientData>()
                        .Where(x => x.MessageId == item.MessageId)
                        .Where(x => x.RecipientType == RecipientType.Cc)
                        .ToList();
                    if (toList.IsNullOrEmpty())
                    {
                        item.CC = toList;
                    }
                }
                {
                    var toList = dbContext.Query<MailMessageRecipientData>()
                        .Where(x => x.MessageId == item.MessageId)
                        .Where(x => x.RecipientType == RecipientType.Bcc)
                        .ToList();
                    if (toList.IsNullOrEmpty())
                    {
                        item.Bcc = toList;
                    }
                }
            });
            return messageList;
        }
        catch (Exception e)
        {
            Trace.WriteLine("QueryMessage Error" + e);
            return new List<MailMessageData>();
        }
    }

    public async Task<List<MailFolderData>> QueryFolderByTreeAsync(string RootFolderId, MailType MailType)
    {
        using var db = GetContext();
        var treeAsync = await db.Query<MailFolderData>()
            .Where(x => x.ParentFolderId == RootFolderId)
            .Where(x => x.MailType == MailType)
            .ToListAsync();

        foreach (var mailFolderData in treeAsync.Where(mailFolderData => mailFolderData.ChildFolderCount > 0))
        {
            mailFolderData.ChildFolders =
                await QueryFolderByTreeAsync(mailFolderData.Id, MailType);
        }

        return treeAsync;
    }

    public DbOperationEvent OperationEvent()
    {
        return GetContext().GetDbOperationEvent();
    }
}