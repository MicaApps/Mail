﻿using System;

namespace Mail.Services.Data
{
    public interface IMailMessageAttachmentData
    {
        public string Name { get; }

        public string Id { get; }

        public string ContentType { get; }

        public ulong AttachmentSize { get; }

        public DateTimeOffset LastModifiedDate { get; }
    }
}