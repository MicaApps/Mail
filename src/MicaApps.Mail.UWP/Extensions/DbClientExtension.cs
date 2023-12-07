#nullable enable    
using Chloe;
using Chloe.Exceptions;
using Mail.Services.Data.Enums;

namespace Mail.Extensions;

/// <summary>
///     comment<br />
///     <br />
///     创建者: GaN<br />
///     创建时间: 2023/06/14
/// </summary>
public static class DbClientExtension
{
    private static readonly DbOperationEvent DbOperationEvent = new();

    public static DbOperationEvent GetDbOperationEvent(this IDbContext Context)
    {
        return DbOperationEvent;
    }

    public static void OnEvent(this IDbContext Context, object Entity, OperationType Type)
    {
        DbOperationEvent.OnExecEvent(Entity, Type);
    }

    public static TEntity? InsertOrUpdate<TEntity>(this IDbContext Context, TEntity Entity)
    {
        try
        {
            var insertAsync = Context.Insert(Entity);
            DbOperationEvent.OnExecEvent(Entity, OperationType.Insert);
            return insertAsync;
        }
        catch (ChloeException e)
        {
            var updateAsync = Context.Update(Entity);
            if (updateAsync > 0)
            {
                DbOperationEvent.OnExecEvent(Entity, OperationType.Update);
                return Entity;
            }
        }

        return default;
    }
}