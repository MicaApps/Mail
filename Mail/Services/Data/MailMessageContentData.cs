using System;
using Mail.Interfaces;
using SqlSugar;

namespace Mail.Services.Data;

[SugarTable]
public sealed class MailMessageContentData : DbEntity
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageContentData()
    {
    }

    [SugarColumn(IsPrimaryKey = true)] public new string Id { get; set; }

    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string Content { get; set; }

    public string ContentPreview { get; set; }

    public MailMessageContentType ContentType { get; set; }

    public MailMessageContentData(string Content, string ContentPreview, MailMessageContentType ContentType)
    {
        this.Content = Content;
        this.ContentPreview = ContentPreview;
        this.ContentType = ContentType;
    }

    public static MailMessageContentData Empty()
    {
        return new MailMessageContentData(string.Empty, string.Empty, MailMessageContentType.Text);
    }
}