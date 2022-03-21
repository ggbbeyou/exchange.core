using System.Linq.Expressions;
using Com.Db;
using Com.Db.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Db:
/// </summary>
public class WalletService
{

    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    public WalletService()
    {
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;

    }





    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public MarketInfo? GetMarketBySymbol(string symbol)
    {
        return this.db.MarketInfo.FirstOrDefault(P => P.symbol == symbol);
    }

}