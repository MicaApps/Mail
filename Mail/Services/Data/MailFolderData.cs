using System.Collections.Generic;
using System.Linq;

namespace Mail.Services.Data
{
    internal sealed class MailFolderData
    {
        public string Id { get; }

        public uint MessageCount { get; }

        public IReadOnlyList<MailFolderData> ChildFolders { get; }

        public MailFolderData(string Id, uint MessageCount, IEnumerable<MailFolderData> ChildFolders)
        {
            this.Id = Id;
            this.MessageCount = MessageCount;
            this.ChildFolders = ChildFolders is IReadOnlyList<MailFolderData> ReadOnlyChildFolders ? ReadOnlyChildFolders : ChildFolders.ToArray();
        }
    }
}
