using System;
using System.Collections.Generic;
using Mail.Interfaces;
using Newtonsoft.Json;
using SqlSugar;

namespace Mail.Services.Data;

[SugarTable]
public sealed class MailMessageData : DbEntity
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageData()
    {
    }

    public string InferenceClassification { get; }
    public string FolderId { get; set; }
    public string Title { get; set; }

    [SugarColumn(IsPrimaryKey = true)] public new string Id { get; set; }

    public DateTimeOffset? SentTime { get; set; }

    [JsonIgnore] public string SenderId => Sender.Address;

    [Navigate(NavigateType.OneToOne, nameof(SenderId))]
    public MailMessageRecipientData Sender { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(Id))]
    public IList<MailMessageRecipientData> To { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(Id))]
    public IList<MailMessageRecipientData> CC { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(Id))]
    public IList<MailMessageRecipientData> Bcc { get; set; }

    [SugarColumn(IsIgnore = true)] public IList<IMailMessageAttachmentData> Attachments { get; private set; }

    [Navigate(NavigateType.OneToOne, nameof(Id))]
    public MailMessageContentData Content { get; private set; }

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
        this.SentTime = SentTime;
        this.Sender = Sender;
        this.Sender.Id = Id;

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