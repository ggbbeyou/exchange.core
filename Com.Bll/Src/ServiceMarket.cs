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
    /// 获取交易对基本信息
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
    /// 获取交易对基本信息
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
                    return db.Market.AsNoTracking().Where(P => symbol.Contains(P.symbol)).OrderBy(P => P.sort).ToList();
                }
            }
        }
    }

    /// <summary>
    /// 交易量转换
    /// </summary>
    /// <param name="amount">交易量</param>
    /// <param name="amount_multiple">最小交易量倍数</param>
    /// <returns></returns>
    public decimal ConvertAmount(decimal amount, decimal amount_multiple)
    {
        return ((int)(amount / amount_multiple)) * amount_multiple;
    }

    /// <summary>
    /// 交易价转换
    /// </summary>
    /// <param name="price">交易价</param>
    /// <param name="price_places">价格最小位数(截断)</param>
    /// <returns></returns>
    public decimal ConvertPrice(decimal price, int price_places)
    {
        return Math.Round(price, price_places, MidpointRounding.ToNegativeInfinity);
    }
}