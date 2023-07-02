using System;
using FreeSql.DataAnnotations;

namespace Mail.Services.Data;

[Table]
public sealed class MailMessageContentData
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageContentData()
    {
    }

    [Column(IsPrimary = true)] public string Id { get; set; }

    [Column(StringLength = -1)] public string Content { get; set; }

    public string ContentPreview { get; set; }

    public MailMessageContentType ContentType { get; set; }

    public MailMessageContentData(string MessageId, string Content, string ContentPreview,
        MailMessageContentType ContentType)
    {
        Id = MessageId;
        this.Content = Content;
        this.ContentPreview = ContentPreview;
        this.ContentType = ContentType;
    }

    public static MailMessageContentData Empty()
    {
        return new MailMessageContentData(string.Empty, string.Empty, string.Empty, MailMessageContentType.Text);
    }
}