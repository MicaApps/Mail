using System.Collections.Specialized;
using Mail.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/06/14
/// </summary>
public static class ObservableCollectionExtension<TU> where TU : DbEntity, new()
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Sender">The object that raised the event.</param>
    /// <param name="Args"></param>
    public static async void CollectionChanged_DbHandle(object Sender,
        NotifyCollectionChangedEventArgs Args)
    {
        var client = App.Services.GetService<ISqlSugarClient>()!;
        foreach (var item in Args.NewItems)
        {
            if (item is not TU entity) continue;

            switch (Args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                    await client.SaveOrUpdate(entity);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    await client.Deleteable(entity).ExecuteCommandAsync();
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                default:
                    break;
            }
        }
    }
}