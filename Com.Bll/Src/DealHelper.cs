using System.Linq.Expressions;
using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Com.Bll;
public class DealHelper
{
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    public DealHelper(FactoryConstant constant)
    {
        this.constant = constant;
        // AddTest();
    }

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
        return this.constant.db.Deal.Where(predicate).OrderBy(P => P.time).ToList();
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
            var sql = from deal in this.constant.db.Deal.Where(predicate)
                      group deal by EF.Functions.DateDiffMinute(KlineService.instance.system_init, deal.time) into g
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
                          time_start = KlineService.instance.system_init.AddMinutes(g.Key),
                          time_end = KlineService.instance.system_init.AddMinutes(g.Key + 1).AddMilliseconds(-1),
                          time = DateTimeOffset.UtcNow,
                      };
            return sql.ToList();
        }
        catch (Exception ex)
        {
            this.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
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
            var sql = from deal in this.constant.db.Deal.Where(predicate)
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
            this.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

    /// <summary>
    /// 添加或保存交易记录
    /// </summary>
    /// <param name="deals"></param>
    public int AddOrUpdateDeal(List<Deal> deals)
    {
        List<Deal> temp = this.constant.db.Deal.Where(P => deals.Select(Q => Q.trade_id).Contains(P.trade_id)).ToList();
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
                this.constant.db.Deal.Add(deal);
            }
        }
        return this.constant.db.SaveChanges();
    }

    public void AddTest()
    {
        Random r = new Random();
        for (int i = 0; i < 2000; i++)
        {
            decimal price = r.NextInt64(1, 10);
            decimal amount = r.NextInt64(1, 10);
            this.constant.db.Set<Deal>().Add(new Deal
            {
                trade_id = this.constant.worker.NextId(),
                market = "btc/usdt",
                amount = amount,
                price = price,
                total = amount * price,
                trigger_side = E_OrderSide.buy,
                bid_id = this.constant.worker.NextId(),
                ask_id = this.constant.worker.NextId(),
                time = DateTimeOffset.UtcNow.AddMinutes(-r.NextInt64(0, 100)),
            });
        }
        this.constant.db.SaveChanges();
    }
}