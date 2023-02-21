using System.Collections.Generic;
using System.Linq;

namespace Mail.Class.Data
{
    internal sealed class MailFolderData
    {
        public IReadOnlyList<MailFolderData> ChildFolders { get; }

        public IReadOnlyList<MailMessageData> MessageCollection { get; }

        public MailFolderData(IEnumerable<MailFolderData> ChildFolders, IEnumerable<MailMessageData> MessageCollection)
        {
            this.ChildFolders = ChildFolders.ToArray();
            this.MessageCollection = MessageCollection.ToArray();
        }
    }
}
