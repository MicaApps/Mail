using Mail.Services.Data.Enums;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/15
/// </summary>
public class DbOperationEvent
{
    public delegate void DataExecuting(object entity, OperationType Type);

    public event DataExecuting? ExecEvent;

    public void OnExecEvent(object Entity, OperationType Type)
    {
        ExecEvent?.Invoke(Entity, Type);
    }
}