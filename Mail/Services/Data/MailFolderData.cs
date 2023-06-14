using System.Collections.ObjectModel;
using Mail.Extensions;
using Mail.Interfaces;
using Mail.Services.Data.Enums;
using SqlSugar;

namespace Mail.Services.Data
{
    [SugarTable]
    internal class MailFolderData : DbEntity
    {
        public MailFolderData()
        {
        }

        [SugarColumn(IsPrimaryKey = true)] public new string Id { get; set; }

        public string Name { get; set; }

        public MailFolderType Type { get; set; }
        public bool IsHidden { get; set; }
        [SugarColumn(IsIgnore = true)] public ObservableCollection<MailFolderData> ChildFolders { get; set; }

        public MailType MailType { get; set; }

        /// <summary>
        /// 最高级为""
        /// </summary>
        public string ParentFolderId { get; set; } = "";

        public int ChildFolderCount { get; set; }

        public MailFolderData(string id, string name, MailFolderType type,
            ObservableCollection<MailFolderData> ChildFolders,
            MailType MailType)
        {
            Id = id;
            Name = name;
            Type = type;
            this.ChildFolders = ChildFolders;
            this.ChildFolders.CollectionChanged +=
                ObservableCollectionExtension<MailFolderData>.CollectionChanged_DbHandle;
            this.MailType = MailType;
        }
    }
}