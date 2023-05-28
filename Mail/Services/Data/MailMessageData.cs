using System;
using System.Collections.Generic;

namespace Mail.Services.Data
{
    public sealed class MailMessageData
    {
        public string Title { get; set; }

        public string Id { get; set; }

        public DateTimeOffset? SentTime { get; set; }

        public MailMessageRecipientData Sender { get; set; }

        public IList<MailMessageRecipientData> To { get; set; }

        public IList<MailMessageRecipientData> CC { get; set; }

        public IList<MailMessageRecipientData> Bcc { get; set; }

        public IList<IMailMessageAttachmentData> Attachments { get; private set; }

        public MailMessageContentData Content { get; private set; }

        public MailMessageData(string Title, string Id, DateTimeOffset? SentTime, MailMessageRecipientData Sender,
            IEnumerable<MailMessageRecipientData> To, IEnumerable<MailMessageRecipientData> Cc,
            IEnumerable<MailMessageRecipientData> Bcc, MailMessageContentData Content,
            IEnumerable<MailMessageAttachmentData> Attachments)
        {
            this.Title = Title;
            this.Id = Id;
            this.SentTime = SentTime;
            this.Sender = Sender;
            this.To = new List<MailMessageRecipientData>(To);
            this.CC = new List<MailMessageRecipientData>(Cc);
            this.Bcc = new List<MailMessageRecipientData>(Bcc);
            this.Attachments = new List<IMailMessageAttachmentData>(Attachments);
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
                To = new List<MailMessageRecipientData>(),
                CC = new List<MailMessageRecipientData>(),
                Bcc = new List<MailMessageRecipientData>(),
                Attachments = new List<IMailMessageAttachmentData>()
            };
        }
    }
}