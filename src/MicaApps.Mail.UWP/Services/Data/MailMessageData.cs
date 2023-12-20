using System;
using System.Collections.Generic;
using Chloe.Annotations;
using Mail.Services.Data.Enums;

namespace Mail.Services.Data;

[Table]
public sealed class MailMessageData
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageData()
    {
    }

    public MailMessageData(string FolderId, string Title, string MessageId, DateTimeOffset? SentTime,
        MailMessageRecipientData Sender,
        IEnumerable<MailMessageRecipientData> To, IEnumerable<MailMessageRecipientData> Cc,
        IEnumerable<MailMessageRecipientData> Bcc, MailMessageContentData Content,
        IEnumerable<MailMessageAttachmentData> Attachments, string InferenceClassification = "Focused")
    {
        this.InferenceClassification = InferenceClassification;
        this.FolderId = FolderId;
        this.Title = Title;
        this.MessageId = MessageId;
        this.SentTime = SentTime?.UtcDateTime;
        this.Sender = Sender;
        this.Sender.MessageId = MessageId;
        Sender.RecipientType = RecipientType.Sender;

        this.To = new List<MailMessageRecipientData>(To);
        this.CC = new List<MailMessageRecipientData>(Cc);
        this.Bcc = new List<MailMessageRecipientData>(Bcc);
        this.Attachments = new List<IMailMessageAttachmentData>(Attachments);
        this.Content = Content;
    }

    public MailMessageData(string Title, string MessageId, DateTimeOffset? SentTime, MailMessageRecipientData Sender)
    {
        this.Title = Title;
        this.MessageId = MessageId;
        this.SentTime = SentTime?.UtcDateTime;
        this.Sender = Sender;
    }

    [Column(IsPrimaryKey = true)] public string MessageId { get; set; }

    public string InferenceClassification { get; set; }
    public string FolderId { get; set; }
    public string Title { get; set; }

    public DateTime? SentTime { get; set; }

    [Navigation(nameof(MessageId))] public MailMessageRecipientData Sender { get; set; }

    [Navigation(nameof(MessageId))] public IList<MailMessageRecipientData> To { get; set; }

    [Navigation(nameof(MessageId))] public IList<MailMessageRecipientData> CC { get; set; }

    [Navigation(nameof(MessageId))] public IList<MailMessageRecipientData> Bcc { get; set; }
    [NotMapped] public IList<IMailMessageAttachmentData> Attachments { get; private set; }
    [Navigation(nameof(MessageId))] public MailMessageContentData Content { get; set; }

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