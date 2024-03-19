using System;

namespace Mail.Models
{
    public interface IMailMessageAttachment
    {
        public string Name { get; }

        public string Id { get; }

        public string ContentType { get; }

        public ulong AttachmentSize { get; }

        public DateTimeOffset LastModifiedDate { get; }
    }
}