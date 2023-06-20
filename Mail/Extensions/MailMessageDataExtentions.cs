using System.Collections.Generic;
using Mail.Services.Data;
using Mail.Services.Data.Enums;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/18
/// </summary>
public static class MailMessageDataExtentions
{
    public static IEnumerable<MailMessageRecipientData> GetRecipientData(this MailMessageData Data)
    {
        var msgId = Data.Id;
        foreach (var to in Data.To)
        {
            to.Id = msgId;
            to.RecipientType = RecipientType.To;
            yield return to;
        }

        foreach (var to in Data.CC)
        {
            to.Id = msgId;
            to.RecipientType = RecipientType.Cc;
            yield return to;
        }

        foreach (var to in Data.Bcc)
        {
            to.Id = msgId;
            to.RecipientType = RecipientType.Bcc;
            yield return to;
        }

        Data.Sender.Id = msgId;
        Data.Sender.RecipientType = RecipientType.Sender;
        yield return Data.Sender;
    }
}