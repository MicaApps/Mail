using Mail.Interfaces;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/15
/// </summary>
public class DbOperationEvent<T> where T : DbEntity
{
    public delegate void DbSave(T entity);

    public delegate void DbUpdate(T entity);

    public delegate void DbRemove(T entity);

    public event DbSave? SaveEvent;
    public event DbUpdate? UpdateEvent;
    public event DbRemove? RemoveEvent;

    public virtual void OnSave(T Entity)
    {
        SaveEvent?.Invoke(Entity);
    }

    public virtual void OnUpdate(T Entity)
    {
        UpdateEvent?.Invoke(Entity);
    }

    public virtual void OnRemove(T Entity)
    {
        RemoveEvent?.Invoke(Entity);
    }
}