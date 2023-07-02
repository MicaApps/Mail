using System;
using System.Collections.Generic;
using FreeSql.DataAnnotations;
using Mail.Services.Data.Enums;

namespace Mail.Services.Data;

[Table]
public sealed class MailMessageData
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageData()
    {
    }

    [Column(IsPrimary = true)] public string Id { get; set; }
    [Column(StringLength = 128)] public string InferenceClassification { get; set; }
    public string FolderId { get; set; }
    public string Title { get; set; }

    public DateTime? SentTime { get; set; }

    [Navigate(nameof(Id))] public MailMessageRecipientData Sender { get; set; }

    [Navigate(nameof(Id))] public IList<MailMessageRecipientData> To { get; set; }

    [Navigate(nameof(Id))] public IList<MailMessageRecipientData> CC { get; set; }

    [Navigate(nameof(Id))] public IList<MailMessageRecipientData> Bcc { get; set; }

    public IList<IMailMessageAttachmentData> Attachments { get; private set; }
    [Navigate(nameof(Id))] public MailMessageContentData Content { get; set; }

    public MailMessageData(string FolderId, string Title, string Id, DateTimeOffset? SentTime,
        MailMessageRecipientData Sender,
        IEnumerable<MailMessageRecipientData> To, IEnumerable<MailMessageRecipientData> Cc,
        IEnumerable<MailMessageRecipientData> Bcc, MailMessageContentData Content,
        IEnumerable<MailMessageAttachmentData> Attachments, string InferenceClassification = "Focused")
    {
        this.InferenceClassification = InferenceClassification;
        this.FolderId = FolderId;
        this.Title = Title;
        this.Id = Id;
        this.SentTime = SentTime?.UtcDateTime;
        this.Sender = Sender;
        this.Sender.Id = Id;
        Sender.RecipientType = RecipientType.Sender;

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
        this.SentTime = SentTime?.UtcDateTime;
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