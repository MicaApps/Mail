﻿using System.Collections.Generic;
using System;
using System.Linq;

namespace Mail.Services.Data
{
    internal sealed class MailMessageData
    {
        public string Title { get; }

        public DateTimeOffset? SentTime { get; }

        public MailMessageRecipientData Sender { get; }

        public IReadOnlyList<MailMessageRecipientData> To { get; }

        public IReadOnlyList<MailMessageRecipientData> CC { get; }

        public IReadOnlyList<MailMessageRecipientData> Bcc { get; }

        public IReadOnlyList<MailMessageAttachmentData> Attachments { get; }

        public MailMessageContentData Content { get; }

        public MailMessageData(string Title, DateTimeOffset? SentTime, MailMessageRecipientData Sender, IEnumerable<MailMessageRecipientData> To, IEnumerable<MailMessageRecipientData> CC, IEnumerable<MailMessageRecipientData> Bcc, MailMessageContentData Content, IEnumerable<MailMessageAttachmentData> Attachments)
        {
            this.Title = Title;
            this.SentTime = SentTime;
            this.Sender = Sender;
            this.To = To.ToArray();
            this.CC = CC.ToArray();
            this.Bcc = Bcc.ToArray();
            this.Attachments = Attachments.ToArray();
            this.Content = Content;
        }
    }
}