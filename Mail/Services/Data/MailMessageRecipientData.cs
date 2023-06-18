using System;
using Mail.Interfaces;
using Mail.Services.Data.Enums;
using Newtonsoft.Json;
using SqlSugar;

namespace Mail.Services.Data;

[SugarTable]
public sealed class MailMessageRecipientData : DbEntity
{
    [Obsolete("这是给框架用的", true)]
    public MailMessageRecipientData()
    {
    }

    public string Name { get; set; }
    [SugarColumn(IsPrimaryKey = true)] public string Address { get; set; }

    /// <summary>
    /// 关联的邮件消息Id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    public new string Id { get; set; }

    [SugarColumn(IsPrimaryKey = true)] public RecipientType RecipientType { get; set; }

    /// <summary>
    /// Outlook Message Compatible
    /// </summary>
    [JsonProperty("EmailAddress")]
    [SugarColumn(IsIgnore = true)]
    public object EmailAddress => new { Name, Address };

    public MailMessageRecipientData(string Name, string Address)
    {
        this.Name = Name;
        this.Address = Address;
    }
}