using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mail.Interfaces;
using SqlSugar;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/14
/// </summary>
public static class DbClientExtension
{
    private static readonly DbOperationEvent<DbEntity> DbOperationEvent = new();

    /// <summary>
    /// 只有扩展方法会调用, 懒得写ISqlSugarClient的AOP
    /// </summary>
    /// <param name="Client"></param>
    /// <returns></returns>
    public static DbOperationEvent<DbEntity> GetDbOperationEvent(this ISqlSugarClient Client)
    {
        return DbOperationEvent;
    }

    public static async Task<int> SaveOrUpdate<T>(this ISqlSugarClient Client, T entity,
        CancellationToken CancellationToken = default) where T : DbEntity, new()
    {
        var x = await Client.Storageable(entity).WhereColumns(x => x.Id).ToStorageAsync();
        var executeCommandAsync = await x.AsInsertable.ExecuteCommandAsync(CancellationToken);
        if (executeCommandAsync > 0)
        {
            DbOperationEvent.OnSave(entity);
            return executeCommandAsync;
        }

        var commandAsync = await x.AsUpdateable.ExecuteCommandAsync(CancellationToken);
        if (commandAsync > 0)
        {
            DbOperationEvent.OnUpdate(entity);
        }

        return commandAsync;
    }

    public static async Task<int> SaveOrUpdate<T>(this ISqlSugarClient Client, IEnumerable<T> entity,
        CancellationToken CancellationToken = default) where T : DbEntity, new()
    {
        var dbEntities = entity.ToList();
        var x = await Client.Storageable(dbEntities).WhereColumns(x => x.Id).ToStorageAsync();
        var executeCommandAsync = await x.AsInsertable.ExecuteCommandAsync(CancellationToken);
        if (executeCommandAsync > 0)
        {
            foreach (var e in dbEntities) DbOperationEvent.OnSave(e);
            return executeCommandAsync;
        }

        var commandAsync = await x.AsUpdateable.ExecuteCommandAsync(CancellationToken);
        if (commandAsync > 0)
            foreach (var e in dbEntities)
                DbOperationEvent.OnUpdate(e);

        return commandAsync;
    }

    public static async Task<int> Remove<T>(this ISqlSugarClient Client, T entity) where T : DbEntity, new()
    {
        var result = await Client.Deleteable<T>().WhereColumns(entity, x => x.Id).ExecuteCommandAsync();
        if (result > 0)
        {
            DbOperationEvent.OnRemove(entity);
        }

        return result;
    }
}