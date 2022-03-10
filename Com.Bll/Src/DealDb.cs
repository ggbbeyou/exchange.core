using System.Linq.Expressions;
using Com.Db;
using Com.Model.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Db:交易记录
/// </summary>
public class DealDb
{
    /// <summary>
    /// 获取交易记录
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Deal> GetDeals(string market, DateTimeOffset? start, DateTimeOffset? end)
    {
        Expression<Func<Deal, bool>> predicate = P => P.market == market;
        if (start != null)
        {
            predicate = predicate.And(P => start <= P.time);
        }
        if (end != null)
        {
            predicate = predicate.And(P => P.time <= end);
        }
        return FactoryService.instance.constant.db.Deal.Where(predicate).OrderBy(P => P.time).ToList();
    }

    /// <summary>
    /// 交易记录转换成一分钟K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Kline>? GetKlinesMin1ByDeal(string market, DateTimeOffset? start, DateTimeOffset? end)
    {
        Expression<Func<Deal, bool>> predicate = P => P.market == market;
        if (start != null)
        {
            predicate = predicate.And(P => start <= P.time);
        }
        if (end != null)
        {
            predicate = predicate.And(P => P.time <= end);
        }
        try
        {
            var sql = from deal in FactoryService.instance.constant.db.Deal.Where(predicate)
                      group deal by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, deal.time) into g
                      select new Kline
                      {
                          market = market,
                          amount = g.Sum(P => P.amount),
                          count = g.Count(),
                          total = g.Sum(P => P.total),
                          open = g.OrderBy(P => P.time).First().price,
                          close = g.OrderBy(P => P.time).Last().price,
                          low = g.Min(P => P.price),
                          high = g.Max(P => P.price),
                          type = E_KlineType.min1,
                          time_start = FactoryService.instance.system_init.AddMinutes(g.Key),
                          time_end = FactoryService.instance.system_init.AddMinutes(g.Key + 1).AddMilliseconds(-1),
                          time = DateTimeOffset.UtcNow,
                      };
            return sql.ToList();
        }
        catch (Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

    /// <summary>
    /// 交易记录转换成K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public Kline? GetKlinesByDeal(string market, E_KlineType type, DateTimeOffset start, DateTimeOffset? end)
    {
        Expression<Func<Deal, bool>> predicate = P => P.market == market && start <= P.time;
        if (end != null)
        {
            predicate = predicate.And(P => P.time <= end);
        }
        try
        {
            var sql = from deal in FactoryService.instance.constant.db.Deal.Where(predicate)
                      group deal by deal.market into g
                      select new Kline
                      {
                          market = market,
                          amount = g.Sum(P => P.amount),
                          count = g.Count(),
                          total = g.Sum(P => P.total),
                          open = g.OrderBy(P => P.time).First().price,
                          close = g.OrderBy(P => P.time).Last().price,
                          low = g.Min(P => P.price),
                          high = g.Max(P => P.price),
                          type = type,
                          time_start = start,
                          time_end = g.OrderBy(P => P.time).Last().time,
                          time = DateTimeOffset.UtcNow,
                      };
            return sql.FirstOrDefault();
        }
        catch (Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

    /// <summary>
    /// 添加或保存交易记录
    /// </summary>
    /// <param name="deals"></param>
    public int AddOrUpdateDeal(List<Deal> deals)
    {
        List<Deal> temp = FactoryService.instance.constant.db.Deal.Where(P => deals.Select(Q => Q.trade_id).Contains(P.trade_id)).ToList();
        foreach (var deal in deals)
        {
            var temp_deal = temp.FirstOrDefault(P => P.trade_id == deal.trade_id);
            if (temp_deal != null)
            {
                temp_deal.price = deal.price;
                temp_deal.amount = deal.amount;
                temp_deal.total = deal.total;
                temp_deal.time = deal.time;
            }
            else
            {
                FactoryService.instance.constant.db.Deal.Add(deal);
            }
        }
        return FactoryService.instance.constant.db.SaveChanges();
    } 

}