﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Chloe;
using Chloe.Annotations;
using Mail.Extensions;
using Mail.Services.Data.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Mail.Services.Data;

[Table]
public class MailFolderData
{
    [Obsolete("这是给框架用的", true)]
    public MailFolderData()
    {
        ChildFolders = new List<MailFolderData>(0);
    }

    public MailFolderData(string id, string name, MailFolderType type,
        IList<MailFolderData> ChildFolders,
        int? TotalItemCount,
        MailType MailType)
    {
        Id = id;
        Name = name;
        Type = type;
        this.ChildFolders = ChildFolders;
        this.TotalItemCount = TotalItemCount ?? 0;
        this.MailType = MailType;
    }

    [Column(IsPrimaryKey = true)] public string Id { get; set; }

    public string Name { get; set; }

    public MailFolderType Type { get; set; }
    public bool IsHidden { get; set; }
    [Navigation(nameof(ParentFolderId))] public IList<MailFolderData> ChildFolders { get; set; }
    public int TotalItemCount { get; set; }

    public MailType MailType { get; set; }

    /// <summary>
    ///     最高级为""
    /// </summary>
    public string ParentFolderId { get; set; } = "";

    public int ChildFolderCount { get; set; }

    public void RecursionChildFolderToObservableCollection()
    {
        using var client = App.Services.GetService<IDbContext>()!;
        var coll = new ObservableCollection<MailFolderData>();
        coll.AddRange(ChildFolders);

        client.GetDbOperationEvent().ExecEvent += (Entity, DataFilterType) =>
        {
            if (Entity is not MailFolderData data) return;

            if (DataFilterType is OperationType.Insert or OperationType.Update)
            {
                var first = ChildFolders.FirstOrDefault(x => x.Id.Equals(data.Id));
                var indexOf = ChildFolders.IndexOf(first);
                // 如果属于这个文件夹
                var isParentFolder = data.ParentFolderId.Equals(Id);
                // 如果不存在
                if (indexOf == -1 && isParentFolder)
                {
                    ChildFolders.Add(data);
                }
                else
                {
                    if (isParentFolder)
                        ChildFolders[indexOf] = data;
                    else
                        ChildFolders.Remove(first);
                }

                return;
            }

            if (DataFilterType != OperationType.Delete) return;
            if (data.ParentFolderId == Id)
                ChildFolders.Remove(data);
        };

        ChildFolders = coll;
        foreach (var mailFolderData in ChildFolders) mailFolderData.RecursionChildFolderToObservableCollection();
    }
}