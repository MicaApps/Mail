using System;

namespace Mail.Class.Data
{
    internal sealed class MailMessageAttachmentData
    {
        public string Name { get; }

        public string Id { get; }

        public string ContentType { get; }

        public ulong AttachmentSize { get; }

        public DateTimeOffset LastModifiedDate { get; }

        public MailMessageAttachmentData(string Name, string Id, string ContentType = null, ulong AttachmentSize = 0, DateTimeOffset LastModifiedDate = default)
        {
            this.Name = Name;
            this.Id = Id;
            this.ContentType = ContentType;
            this.AttachmentSize = AttachmentSize;
            this.LastModifiedDate = LastModifiedDate;
        }
    }
}
