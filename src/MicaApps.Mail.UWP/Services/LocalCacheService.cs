﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Mail.Extensions;
using Mail.Models;
using Mail.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Windows.Gaming.Input.ForceFeedback;

namespace Mail.Services;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/07/16
/// </summary>
public class LocalCacheService
{
    public LiteDatabaseService DatabaseService { get; }

    public LocalCacheService(IServiceProvider serviceProvider)
    {
        DatabaseService = serviceProvider.GetService<LiteDatabaseService>();
    }

    public void SaveMessage(MailMessage message)
    {
        if (!DatabaseService.MailMessages.Update(message))
            DatabaseService.MailMessages.Insert(message);
    }

    public void SaveFolder(MailFolder folder)
    {
        if (!DatabaseService.MailFolders.Update(folder))
            DatabaseService.MailFolders.Insert(folder);
    }

    public List<MailMessage> QueryMessage(LoadMailMessageOption option)
    {
        var tab = option.IsFocusedTab ? "Focused" : "Other";

        return DatabaseService.MailMessages
            .Query()
            .Where(msg => msg.FolderId == option.FolderId)
            .Where(msg => msg.InferenceClassification == tab)
            .Skip(option.StartIndex)
            .Limit(option.LoadCount)
            .ToEnumerable()
            .OrderByDescending(x => x.SentTime)
            .ToList();
    }

    public List<MailFolder> QueryFolderByTree(string rootFolderId, MailType mailType)
    {
        var folders = DatabaseService.MailFolders
            .Query()
            .Where(folder => folder.ParentFolderId == rootFolderId)
            .ToList();

        foreach (var folder in folders)
            folder.RecurseLoadChildFolders(DatabaseService.MailFolders);

        return folders;
    }

    //public DbOperationEvent OperationEvent()
    //{
    //    return GetContext().GetDbOperationEvent();
    //}
}