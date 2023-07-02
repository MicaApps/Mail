using System;
using FreeSql.DataAnnotations;
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
    /// 关联的邮件消息Id
    /// </summary>
    [Column(IsPrimary = true)]
    public string Id { get; set; }

    public string Name { get; set; }
    [Column(IsPrimary = true)] public string Address { get; set; }

    [Column(IsPrimary = true)] public RecipientType RecipientType { get; set; }

    /// <summary>
    /// Outlook Message Compatible
    /// </summary>
    [JsonProperty("EmailAddress")]
    public object EmailAddress => new { Name, Address };

    public MailMessageRecipientData(string Name, string Address)
    {
        this.Name = Name;
        this.Address = Address;
    }
}