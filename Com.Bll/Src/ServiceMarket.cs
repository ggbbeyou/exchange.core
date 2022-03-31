using System.Linq.Expressions;
using Com.Db;
using Com.Api.Sdk.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Db:交易对
/// </summary>
public class ServiceMarket
{
    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceMarket()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public Market? GetMarketBySymbol(string symbol)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.Market.AsNoTracking().FirstOrDefault(P => P.symbol == symbol);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public List<Market> GetMarketBySymbol(List<string> symbol)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                if (symbol == null || symbol.Count == 0)
                {
                    return db.Market.AsNoTracking().ToList();
                }
                else
                {
                    return db.Market.AsNoTracking().Where(P => symbol.Contains(P.symbol)).ToList();
                }
            }
        }
    }
}