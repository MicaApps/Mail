using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private static readonly DbOperationEvent DbOperationEvent = new();

    /// <summary>
    /// 只有扩展方法会调用, 懒得写ISqlSugarClient的AOP
    /// </summary>
    /// <param name="Client"></param>
    /// <returns></returns>
    public static DbOperationEvent GetDbOperationEvent(this ISqlSugarClient Client)
    {
        return DbOperationEvent;
    }

    public static async Task<int> SaveOrUpdate<T>(this ISqlSugarClient Client, T entity,
        CancellationToken CancellationToken = default) where T : class, new()
    {
        var x = await Client.Storageable(entity).ToStorageAsync();
        var executeCommandAsync = await x.AsInsertable.ExecuteCommandAsync(CancellationToken);
        if (executeCommandAsync > 0)
        {
            return executeCommandAsync;
        }

        var commandAsync = await x.AsUpdateable.ExecuteCommandAsync(CancellationToken);

        return commandAsync;
    }

    public static async Task<int> SaveOrUpdate<T>(this ISqlSugarClient Client, IEnumerable<T> entity,
        CancellationToken CancellationToken = default) where T : class, new()
    {
        var dbEntities = entity.ToList();
        var x = await Client.Storageable(dbEntities).ToStorageAsync();
        var executeCommandAsync = await x.AsInsertable.ExecuteCommandAsync(CancellationToken);
        if (executeCommandAsync > 0)
        {
            return executeCommandAsync;
        }

        var commandAsync = await x.AsUpdateable.ExecuteCommandAsync(CancellationToken);

        return commandAsync;
    }
}