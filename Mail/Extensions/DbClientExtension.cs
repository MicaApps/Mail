using System.Collections.Generic;
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
    public static async Task<int> SaveOrUpdate<T>(this ISqlSugarClient Client, T entity,
        CancellationToken CancellationToken = default) where T : DbEntity, new()
    {
        var x = await Client.Storageable(entity).WhereColumns(x => x.Id).ToStorageAsync();
        var executeCommandAsync = await x.AsInsertable.ExecuteCommandAsync(CancellationToken);
        if (executeCommandAsync > 0)
        {
            return executeCommandAsync;
        }

        var commandAsync = await x.AsUpdateable.ExecuteCommandAsync(CancellationToken);
        return commandAsync;
    }

    public static async Task<int> SaveOrUpdate<T>(this ISqlSugarClient Client, IEnumerable<T> entity,
        CancellationToken CancellationToken = default) where T : DbEntity, new()
    {
        var x = await Client.Storageable(new List<T>(entity)).WhereColumns(x => x.Id).ToStorageAsync();
        var executeCommandAsync = await x.AsInsertable.ExecuteCommandAsync(CancellationToken);
        if (executeCommandAsync > 0)
        {
            return executeCommandAsync;
        }

        var commandAsync = await x.AsUpdateable.ExecuteCommandAsync(CancellationToken);
        return commandAsync;
    }
}