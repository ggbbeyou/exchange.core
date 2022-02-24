using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Com.Bll;
public class KilneHelper
{

    /// <summary>
    /// 常用接口
    /// https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core-example.html
    /// </summary>
    public FactoryConstant constant = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="constant"></param>
    public KilneHelper(FactoryConstant constant)
    {
        this.constant = constant;
        // AddTest();
    }

    /// <summary>
    /// 从数据库统计K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<BaseKline> GetKlines(string market, E_KlineType klineType, BaseKline? last_kline, DateTimeOffset end, TimeSpan span)
    {
        List<BaseKline> result = new List<BaseKline>();
        DateTimeOffset start = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        decimal last_price = 0;
        if (last_kline != null)
        {
            start = last_kline.time_start.Add(span);
            last_price = last_kline.close;
        }
        List<Deal> deals = this.constant.db.Deal.Where(P => P.market == market && start <= P.time && P.time <= end).OrderBy(P => P.time).ToList();
        for (DateTimeOffset i = start; i <= end; i = i.Add(span))
        {
            DateTimeOffset end_time = i.Add(span).AddMilliseconds(-1);
            List<Deal> deal = deals.Where(P => P.time >= i && P.time <= end_time).ToList().ToList();
            if (last_price == 0 && deal.Count == 0)
            {
                continue;
            }
            BaseKline? kline = DealToKline(market, klineType, i, end_time, deal, last_price);
            if (kline != null)
            {
                result.Add(kline);
            }
        }
        return result;
    }

    /// <summary>
    /// 交易记录转换成K线
    /// </summary>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="deals"></param>
    /// <returns></returns>
    public BaseKline? DealToKline(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end, List<Deal> deals, decimal last_price)
    {
        BaseKline kline = new BaseKline();
        if (last_price > 0 && deals.Count == 0)
        {
            kline.market = market;
            kline.type = klineType;
            kline.amount = 0;
            kline.count = 0;
            kline.total = 0;
            kline.open = last_price;
            kline.close = last_price;
            kline.low = last_price;
            kline.high = last_price;
            kline.time_start = start;
            kline.time_end = end;
            kline.time = DateTimeOffset.UtcNow;
            return kline;
        }
        else if (deals.Count > 0)
        {
            deals = deals.OrderBy(P => P.time).ToList();
            kline.market = market;
            kline.type = klineType;
            kline.amount = deals.Sum(P => P.amount);
            kline.count = deals.Count;
            kline.total = deals.Sum(P => P.amount * P.price);
            kline.open = deals[0].price;
            kline.close = deals[deals.Count - 1].price;
            kline.low = deals.Min(P => P.price);
            kline.high = deals.Max(P => P.price);
            kline.time_start = start;
            kline.time_end = end;
            kline.time = DateTimeOffset.UtcNow;
            return kline;
        }
        return null;
    }


    /*

  var sql = from deal in this.constant.db.Deal
                      where deal.market == market && start <= deal.time && deal.time < end
                      orderby deal.time
                      group deal by deal.time.Ticks / span.Ticks into g
                      select new BaseKline
                      {
                          market = market,
                          amount = g.Sum(P => P.amount),
                          count = g.Count(),
                          total = g.Sum(P => P.price * P.amount),
                          open = g.First().price,
                          close = g.Last().price,
                          low = g.Min(P => P.price),
                          high = g.Max(P => P.price),
                          type = klineType,
                          time_start = new DateTimeOffset(g.Key * span.Ticks, TimeSpan.Zero),
                          time_end = new DateTimeOffset((g.Key + 1) * span.Ticks, TimeSpan.Zero),
                          time = DateTimeOffset.UtcNow,
                      };
            result = sql.ToList();

    */

    public void AddTest()
    {
        Random r = new Random();
        for (int i = 0; i < 10; i++)
        {
            this.constant.db.Set<Deal>().Add(new Deal
            {
                trade_id = this.constant.worker.NextId(),
                market = "btc/usdt",
                amount = (decimal)r.NextDouble(),
                price = (decimal)r.NextDouble(),
                total = (decimal)r.NextDouble(),
                trigger_side = E_OrderSide.buy,
                bid_id = this.constant.worker.NextId(),
                ask_id = this.constant.worker.NextId(),
                time = DateTimeOffset.UtcNow.AddDays(-1).AddMinutes(i),
            });
        }
        this.constant.db.SaveChanges();
    }

}