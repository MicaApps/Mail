using System;
using System.Collections;
using System.Collections.Generic;

namespace Mail.Models;

public class MailMessage
{
    public MailMessage()
    {
    }

    public MailMessage(
        string folderId,
        string title,
        string id,
        DateTimeOffset? sentTime,
        MailMessageRecipient sender,
        IEnumerable<MailMessageRecipient> to,
        IEnumerable<MailMessageRecipient> cc,
        IEnumerable<MailMessageRecipient> bcc,
        MailMessageContent content,
        IEnumerable<MailMessageAttachment> attachments,
        string type)
    {
        FolderId = folderId;
        Title = title;
        Id = id;
        SentTime = sentTime?.UtcDateTime;
        Sender = sender;
        To = new List<MailMessageRecipient>(to);
        CC = new List<MailMessageRecipient>(cc);
        Bcc = new List<MailMessageRecipient>(bcc);

        Attachments = new List<IMailMessageAttachment>(attachments);
        Content = content;
    }

    public string Id { get; set; }
    public string InferenceClassification { get; set; }
    public string FolderId { get; set; }
    public string Title { get; set; }
    public DateTime? SentTime { get; set; }

    public MailMessageRecipient Sender { get; set; }
    public List<MailMessageRecipient> To { get; set; }
    public List<MailMessageRecipient> CC { get; set; }
    public List<MailMessageRecipient> Bcc { get; set; }

    public MailMessageContent Content { get; set; }

    [LiteDB.BsonIgnore]
    public List<IMailMessageAttachment> Attachments { get; set; }

    public static MailMessage Empty(MailMessageRecipient sender)
    {
        return new MailMessage()
        {
            SentTime = DateTime.UtcNow,
            Sender = sender,
            Content = new(),
            To = new(),
            CC = new(),
            Bcc = new(),
            Attachments = new(),
        };
    }
}
