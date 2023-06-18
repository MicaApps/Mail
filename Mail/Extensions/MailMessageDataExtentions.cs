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
        foreach (var to in Data.To)
        {
            to.Id = Data.Id;
            to.RecipientType = RecipientType.To;
            yield return to;
        }

        foreach (var to in Data.CC)
        {
            to.Id = Data.Id;
            to.RecipientType = RecipientType.Cc;
            yield return to;
        }

        foreach (var to in Data.Bcc)
        {
            to.Id = Data.Id;
            to.RecipientType = RecipientType.Bcc;
            yield return to;
        }
    }
}