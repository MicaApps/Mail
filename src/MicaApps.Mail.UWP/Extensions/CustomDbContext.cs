using Chloe.Infrastructure;
using Chloe.SQLite;
using Mail.Services.Data.Enums;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Mail.Extensions;

/// <summary>
/// comment<br/>
/// <br/>
/// 创建者: GaN<br/>
/// 创建时间: 2023/07/09
/// </summary>
public class CustomDbContext : SQLiteContext
{
    public CustomDbContext(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
    {

    }

    public CustomDbContext(IDbConnectionFactory dbConnectionFactory, bool concurrencyMode) : base(dbConnectionFactory, concurrencyMode)
    {

    }

    public CustomDbContext(Func<IDbConnection> dbConnectionFactory) : base(dbConnectionFactory)
    {

    }

    public CustomDbContext(Func<IDbConnection> dbConnectionFactory, bool concurrencyMode) : base(dbConnectionFactory, concurrencyMode)
    {

    }

    protected override Task<int> Delete<TEntity>(TEntity entity, string table, bool async)
    {
        this.GetDbOperationEvent().OnExecEvent(entity, OperationType.Delete);
        return base.Delete(entity, table, async);
    }
}