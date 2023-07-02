using Mail.Events;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/14
/// </summary>
public static class DbClientExtension
{
    private static readonly DbOperationEvent DbOperationEvent = new();

    public static DbOperationEvent GetDbOperationEvent(this IFreeSql Client)
    {
        return DbOperationEvent;
    }
}