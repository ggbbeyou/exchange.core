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
    /// 
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
    /// <param name="market"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
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
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Kline>? GetKlinesMin1ByDeal(string market, E_KlineType klineType, DateTimeOffset? start, DateTimeOffset? end)
    {
        List<Kline> klines = new List<Kline>();
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
            var deals = from deal in this.constant.db.Deal.Where(predicate)
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
                            type = klineType,
                            time_start = g.OrderBy(P => P.time).First().time,
                            time_end = g.OrderBy(P => P.time).Last().time,
                            time = DateTimeOffset.UtcNow,
                        };
            klines = deals.ToList();
        }
        catch (Exception ex)
        {
            this.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
            return null;
        }
        return klines;
    }



    public void AddTest()
    {
        Random r = new Random();
        for (int i = 0; i < 5000; i++)
        {
            decimal price = r.NextInt64(2000, 4000);
            decimal amount = r.NextInt64(1, 25);
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
                time = DateTimeOffset.UtcNow.AddMinutes(-i),
            });
        }
        this.constant.db.SaveChanges();
    }
}