using System.Collections;
using FreeSql.Aop;
using FreeSql.Internal.CommonProvider;

namespace Mail.Events;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/15
/// </summary>
public class DbOperationEvent
{
    public delegate void DataExecuting(object entity, CurdType Type);

    public event DataExecuting? ExecEvent;

    public void OnExecEvent(object Entity, CurdType Type)
    {
        if (Type == CurdType.InsertOrUpdate)
        {
            var InsertOrUpdateFiled = Entity.GetType().GetField(nameof(InsertOrUpdateProvider<object>._source));

            var then = (IEnumerable)InsertOrUpdateFiled?.GetValue(Entity);
            foreach (var o in then)
            {
                ExecEvent?.Invoke(o, Type);
            }
        }
        else
        {
            ExecEvent?.Invoke(Entity, Type);
        }
    }
}