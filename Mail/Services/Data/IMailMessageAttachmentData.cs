using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail.Services.Data
{
    internal interface IMailMessageAttachmentData
    {
        public string Name { get; }

        public string Id { get; }

        public string ContentType { get; }

        public ulong AttachmentSize { get; }

        public DateTimeOffset LastModifiedDate { get; }
    }
}
