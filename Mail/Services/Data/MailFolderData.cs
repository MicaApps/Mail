using System.Collections.Generic;

namespace Mail.Services.Data
{
    internal class MailFolderData
    {
        public string Id { get; }

        public string Name { get; }

        public MailFolderType Type { get; }
        public bool IsHidden { get; set; }

        public IList<MailFolderData> ChildFolders { get; }

        public string ParentFolderId { get; set; } = "";
        public int ChildFolderCount { get; set; }

        public MailFolderData(string id, string name, MailFolderType type, IList<MailFolderData> ChildFolders)
        {
            Id = id;
            Name = name;
            Type = type;
            this.ChildFolders = ChildFolders;
        }

        public override string ToString()
        {
            return
                $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Type)}: {Type}, {nameof(IsHidden)}: {IsHidden}, {nameof(ChildFolders)}: {ChildFolders}";
        }
    }
}