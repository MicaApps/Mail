using System.Collections.Generic;
using LiteDB;
using Mail.Models.Enums;
using Microsoft.Graph.Models;

namespace Mail.Models;

public class MailFolder
{
    public MailFolder()
    {
    }

    public MailFolder(string id, string name, MailFolderType type, int totalItemCount, MailType mailType, List<MailFolder> childFolders)
    {
        Id = id;
        Name = name;
        Type = type;
        TotalItemCount = totalItemCount;
        MailType = mailType;
        ChildFolders = childFolders;

        foreach (var childFolder in ChildFolders)
        {
            if (!string.IsNullOrWhiteSpace(childFolder.ParentFolderId))
                throw new System.ArgumentException("One of specified child folders already has a parent folder");

            childFolder.ParentFolderId = this.Id;
        }
    }

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MailFolderType Type { get; set; }
    public bool IsHidden { get; set; }
    public int TotalItemCount { get; set; }
    public MailType MailType { get; set; }
    public string ParentFolderId { get; set; }

    [BsonIgnore]
    //[BsonRef("mailFolders")]
    public List<MailFolder> ChildFolders { get; set; }

    public void LoadChildFolders(ILiteCollection<MailFolder> collection)
    {
        ChildFolders = collection
            .Query()
            .Where(folder => folder.ParentFolderId == this.Id)
            .ToList();
    }

    public void RecurseLoadChildFolders(ILiteCollection<MailFolder> collection)
    {
        LoadChildFolders(collection);

        foreach (var childFolder in ChildFolders)
            childFolder.RecurseLoadChildFolders(collection);
    }
}
