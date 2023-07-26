using Chloe;
using Mail.Services.Data;
using Mail.Services.Data.Enums;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/18
/// </summary>
public static class MailMessageDataExtensions
{
    public static void SaveRecipientData(this MailMessageData Data,
        IDbContext DbContext)
    {
        var msgId = Data.MessageId;
        foreach (var to in Data.To)
        {
            to.MessageId = msgId;
            to.RecipientType = RecipientType.To;
            DbContext.InsertOrUpdate(to);
        }

        foreach (var to in Data.CC)
        {
            to.MessageId = msgId;
            to.RecipientType = RecipientType.Cc;
            DbContext.InsertOrUpdate(to);
        }

        foreach (var to in Data.Bcc)
        {
            to.MessageId = msgId;
            to.RecipientType = RecipientType.Bcc;
            DbContext.InsertOrUpdate(to);
        }

        Data.Sender.MessageId = msgId;
        Data.Sender.RecipientType = RecipientType.Sender;
        DbContext.InsertOrUpdate(Data.Sender);
    }
}