using System.Collections.Generic;
using System.Linq;

namespace Mail.Services.Data
{
    internal sealed class MailFolderDetailData
    {
        public string Id { get; }

        public uint MessageCount { get; }

        public IReadOnlyList<MailFolderDetailData> ChildFolders { get; }

        public MailFolderDetailData(string Id, uint MessageCount, IEnumerable<MailFolderDetailData> ChildFolders)
        {
            this.Id = Id;
            this.MessageCount = MessageCount;
            this.ChildFolders = ChildFolders as IReadOnlyList<MailFolderDetailData> ?? ChildFolders.ToArray();
        }
    }
}