namespace Mail.Services.Data
{
    internal class MailFolderData
    {
        public string Id { get; }

        public string Name { get; }

        public MailFolderType Type { get; }

        public MailFolderData[] ChildFolders { get; }

        public MailFolderData(string id, string name, MailFolderType type, MailFolderData[] ChildFolders)
        {
            Id = id;
            Name = name;
            Type = type;
            this.ChildFolders = ChildFolders;
        }
    }
}