using System.Collections.Generic;
using System;
using System.Linq;

namespace Mail.Services.Data
{
    internal sealed class MailMessageData
    {
        public string Title { get; set; }

        public string Id { get; }

        public DateTimeOffset? SentTime { get; }

        public MailMessageRecipientData Sender { get; }

        public IReadOnlyList<MailMessageRecipientData> To { get; private set; }

        public IReadOnlyList<MailMessageRecipientData> CC { get; private set; }

        public IReadOnlyList<MailMessageRecipientData> Bcc { get; private set; }

        public IReadOnlyList<IMailMessageAttachmentData> Attachments { get; private set; }

        public MailMessageContentData Content { get; private set; }

        public MailMessageData(string Title, string Id, DateTimeOffset? SentTime, MailMessageRecipientData Sender, IEnumerable<MailMessageRecipientData> To, IEnumerable<MailMessageRecipientData> CC, IEnumerable<MailMessageRecipientData> Bcc, MailMessageContentData Content, IEnumerable<MailMessageAttachmentData> Attachments)
        {
            this.Title = Title;
            this.Id = Id;
            this.SentTime = SentTime;
            this.Sender = Sender;
            this.To = To.ToArray();
            this.CC = CC.ToArray();
            this.Bcc = Bcc.ToArray();
            this.Attachments = Attachments.ToArray();
            this.Content = Content;
        }

        public MailMessageData(string Title, string Id, DateTimeOffset? SentTime, MailMessageRecipientData Sender)
        {
            this.Title = Title;
            this.Id = Id;
            this.SentTime = SentTime;
            this.Sender = Sender;
        }

        public static MailMessageData Empty(MailMessageRecipientData Sender)
        {
            return new MailMessageData(string.Empty, string.Empty, DateTimeOffset.Now, Sender)
            {
                Content = MailMessageContentData.Empty(),
                To = Array.Empty<MailMessageRecipientData>(),
                CC = Array.Empty<MailMessageRecipientData>(),
                Bcc = Array.Empty<MailMessageRecipientData>(),
                Attachments = Array.Empty<IMailMessageAttachmentData>()
            };
        }
    }
}
