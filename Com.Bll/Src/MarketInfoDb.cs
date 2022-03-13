using System.Linq.Expressions;
using Com.Db;
using Com.Db.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Db:交易对
/// </summary>
public class MarketInfoDb
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="string"></typeparam>
    /// <typeparam name="MarketInfo"></typeparam>
    /// <returns></returns>
    public Dictionary<string, MarketInfo> market_info_list = new Dictionary<string, MarketInfo>();

    public MarketInfoDb()
    {
        Update();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Update()
    {
        market_info_list = FactoryService.instance.constant.db.MarketInfo.ToDictionary(x => x.symbol, x => x);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public long GetMarketBySymbol(string symbol)
    {
        if (market_info_list.ContainsKey(symbol))
        {
            return market_info_list[symbol].market;
        }
        return 0;
    }

}