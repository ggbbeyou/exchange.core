using System.Linq.Expressions;
using Com.Db;
using Com.Api.Sdk.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Com.Api.Sdk.Models;
using StackExchange.Redis;
using Newtonsoft.Json;

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
    /// <param name="symbol">交易对</param>
    /// <returns></returns>
    public Res<List<ResMarket>> Market(List<string> symbol)
    {
        Res<List<ResMarket>> res = new Res<List<ResMarket>>();
        List<ResMarket> market = this.GetMarketBySymbol(symbol).ConvertAll(P => new ResMarket()
        {
            market = P.market,
            symbol = P.symbol,
            coin_name_base = P.coin_name_base,
            coin_name_quote = P.coin_name_quote,
            market_type = P.market_type,
            transaction = P.transaction,
            status = P.status,
            places_price = P.places_price,
            places_amount = P.places_amount,
            trade_min = P.trade_min,
            trade_min_market_sell = P.trade_min_market_sell,
            sort = P.sort
        });
        if (market != null)
        {

            res.code = E_Res_Code.ok;
            res.data = market;
        }
        return res;
    }

    /// <summary>
    /// 获取深度行情
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="sz">深度档数,只支持10,50,200</param>
    /// <returns></returns>
    public Res<ResDepth?> Depth(string symbol, int sz = 50)
    {
        Res<ResDepth?> res = new Res<ResDepth?>();
        if (sz != 10 && sz != 50 && sz != 200)
        {
            return res;
        }
        Market? market = this.GetMarketBySymbol(symbol);
        if (market != null)
        {
            RedisValue rv = FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisDepth(market.market), "books" + sz);
            if (!rv.HasValue)
            {
                return res;
            }
            res.code = E_Res_Code.ok;
            res.data = JsonConvert.DeserializeObject<ResDepth>(rv);
        }
        return res;
    }



    ////////////////////////////////////////////////////////////////////////
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


}