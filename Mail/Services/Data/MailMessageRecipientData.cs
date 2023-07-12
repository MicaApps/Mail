using System;
using Chloe.Annotations;
using Mail.Services.Data.Enums;
using Newtonsoft.Json;

namespace Mail.Services.Data;

[Table]
public sealed class MailMessageRecipientData
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageRecipientData()
    {
    }

    /// <summary>
    /// 数据库生成的id
    /// </summary>
    [Column(IsPrimaryKey = true)]
    [AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// 关联的邮件消息Id
    /// </summary>
    public string MessageId { get; set; }

    public string Name { get; set; }
    public string Address { get; set; }

    public RecipientType RecipientType { get; set; }

    /// <summary>
    /// Outlook Message Compatible
    /// </summary>
    [JsonProperty("EmailAddress")]
    [NotMapped]
    public object EmailAddress => new { Name, Address };

    public MailMessageRecipientData(string Name, string Address)
    {
        this.Name = Name;
        this.Address = Address;
    }
}