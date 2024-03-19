using System;

namespace Mail.Models
{
    public sealed class MailMessageAttachment : IMailMessageAttachment
    {
        public string Name { get; }

        public string Id { get; }

        public string ContentType { get; }

        public ulong AttachmentSize { get; }

        public DateTimeOffset LastModifiedDate { get; }

        public MailMessageAttachment(string Name, string Id, string ContentType = null, ulong AttachmentSize = 0,
            DateTimeOffset LastModifiedDate = default)
        {
            this.Name = Name;
            this.Id = Id;
            this.ContentType = ContentType;
            this.AttachmentSize = AttachmentSize;
            this.LastModifiedDate = LastModifiedDate;
        }
    }
}