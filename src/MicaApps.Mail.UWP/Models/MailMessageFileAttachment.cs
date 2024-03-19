using System;

namespace Mail.Models
{
    internal class MailMessageFileAttachment : IMailMessageAttachment
    {
        public string Name { get; }

        public string Id { get; }

        public string ContentType { get; }

        public ulong AttachmentSize { get; }

        public DateTimeOffset LastModifiedDate { get; }

        public byte[] ContentBytes { get; }

        public MailMessageFileAttachment(string Name, string Id, string ContentType = null,
            ulong AttachmentSize = 0, DateTimeOffset LastModifiedDate = default, byte[] ContentBytes = null)
        {
            this.Name = Name;
            this.Id = Id;
            this.ContentType = ContentType;
            this.AttachmentSize = AttachmentSize;
            this.LastModifiedDate = LastModifiedDate;
            this.ContentBytes = ContentBytes;
        }
    }
}