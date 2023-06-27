using Mail.Services.Data;
using SqlSugar;

namespace Mail.Subscribe;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/27
/// </summary>
public class MailMessageEvent
{
    public delegate void Event(DataFilterType Type, MailMessageData Data);

    public event Event? On;

    public void OnEvent(DataFilterType Type, MailMessageData Data)
    {
        On?.Invoke(Type, Data);
    }
}