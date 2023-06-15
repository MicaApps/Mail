using System.Collections.Generic;
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
            ChildFolders = new List<MailFolderData>(0);
        }

        [SugarColumn(IsPrimaryKey = true)] public new string Id { get; set; }

        public string Name { get; set; }

        public MailFolderType Type { get; set; }
        public bool IsHidden { get; set; }

        [SugarColumn(IsIgnore = true)] public IList<MailFolderData> ChildFolders { get; set; }

        public MailType MailType { get; set; }

        /// <summary>
        /// 最高级为""
        /// </summary>
        public string ParentFolderId { get; set; } = "";

        public int ChildFolderCount { get; set; }

        public MailFolderData(string id, string name, MailFolderType type,
            IList<MailFolderData> ChildFolders,
            MailType MailType)
        {
            Id = id;
            Name = name;
            Type = type;
            this.ChildFolders = ChildFolders;
            this.MailType = MailType;
        }

        public void RecursionChildFolderToObservableCollection(ISqlSugarClient Client)
        {
            var coll = new ObservableCollection<MailFolderData>();
            coll.AddRange(ChildFolders);

            Client.GetDbOperationEvent().SaveEvent += Entity =>
            {
                if (Entity is MailFolderData data && data.ParentFolderId == Id)
                {
                    ChildFolders.Add(data);
                }
            };
            Client.GetDbOperationEvent().RemoveEvent += Entity =>
            {
                if (Entity is MailFolderData data && data.ParentFolderId == Id)
                {
                    ChildFolders.Remove(data);
                }
            };
            ChildFolders = coll;
            foreach (var mailFolderData in ChildFolders)
            {
                mailFolderData.RecursionChildFolderToObservableCollection(Client);
            }
        }
    }
}