using System.Linq.Expressions;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Db:K线
/// </summary>
public class KilneDb
{

    /// <summary>
    /// 获取最后一条K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="type">K线类型</param>
    /// <returns></returns>
    public Kline? GetLastKline(long market, E_KlineType type)
    {
        return FactoryService.instance.constant.db.Kline.Where(P => P.market == market && P.type == type).OrderByDescending(P => P.time_start).FirstOrDefault();
    }

    /// <summary>
    ///  从数据库获取K线数据
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="type">K线类型</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Kline> GetKlines(long market, E_KlineType type, DateTimeOffset start, DateTimeOffset end)
    {
        return FactoryService.instance.constant.db.Kline.Where(P => P.market == market && P.type == type && P.time_start >= start && P.time_start <= end).OrderBy(P => P.time_start).ToList();
    }

    /// <summary>
    /// 保存K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="klines"></param>
    /// <returns></returns>
    public int SaveKline(long market, E_KlineType klineType, List<Kline> klines)
    {
        if (klines == null || klines.Count == 0)
        {
            return 0;
        }
        List<Kline> db_kline = FactoryService.instance.constant.db.Kline.Where(P => P.market == market && P.type == klineType && P.time_start >= klines[0].time_start && P.time_end <= klines[klines.Count - 1].time_end).ToList();
        foreach (var item in klines)
        {
            Kline? kline = db_kline.FirstOrDefault(P => P.time_start == item.time_start);
            if (kline == null)
            {
                kline = new Kline();
                kline.id = FactoryService.instance.constant.worker.NextId();
                kline.time_start = item.time_start;
                kline.time_end = item.time_end;
                kline.time = item.time;
                FactoryService.instance.constant.db.Kline.Add(kline);
            }
            kline.market = market;
            kline.type = klineType;
            kline.amount = item.amount;
            kline.count = item.count;
            kline.total = item.total;
            kline.open = item.open;
            kline.close = item.close;
            kline.low = item.low;
            kline.high = item.high;
            kline.time_start = item.time_start;
            kline.time_end = item.time_end;
            kline.time = item.time;
        }
        return FactoryService.instance.constant.db.SaveChanges();
    }

    /// <summary>
    ///  计算出K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="type">K线类型</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Kline>? CalcKlines(long market, E_KlineType type, DateTimeOffset? start, DateTimeOffset? end)
    {
        Expression<Func<Kline, bool>> predicate = P => P.market == market;
        if (start != null)
        {
            predicate = predicate.And(P => start <= P.time_start);
        }
        if (end != null)
        {
            predicate = predicate.And(P => P.time_end <= end);
        }
        try
        {
            switch (type)
            {
                case E_KlineType.min1:
                    return FactoryService.instance.deal_db.GetKlinesMin1ByDeal(market, start, end);
                case E_KlineType.min5:
                    predicate = predicate.And(P => P.type == E_KlineType.min1);
                    var sql5 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                               orderby kline.time_start
                               group kline by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, kline.time_start) / 5 into g
                               select new Kline
                               {
                                   market = market,
                                   amount = g.Sum(P => P.amount),
                                   count = g.Sum(P => P.count),
                                   total = g.Sum(P => P.total),
                                   open = g.OrderBy(P => P.time_start).First().open,
                                   close = g.OrderBy(P => P.time_start).Last().close,
                                   low = g.Min(P => P.low),
                                   high = g.Max(P => P.high),
                                   type = type,
                                   time_start = FactoryService.instance.system_init.AddMinutes(g.Key * 5),
                                   time_end = FactoryService.instance.system_init.AddMinutes((g.Key + 1) * 5).AddMilliseconds(-1),
                                   time = DateTimeOffset.UtcNow,
                               };
                    return sql5.ToList();
                case E_KlineType.min15:
                    predicate = predicate.And(P => P.type == E_KlineType.min5);
                    var sql15 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                orderby kline.time_start
                                group kline by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, kline.time_start) / 15 into g
                                select new Kline
                                {
                                    market = market,
                                    amount = g.Sum(P => P.amount),
                                    count = g.Sum(P => P.count),
                                    total = g.Sum(P => P.total),
                                    open = g.OrderBy(P => P.time_start).First().open,
                                    close = g.OrderBy(P => P.time_start).Last().close,
                                    low = g.Min(P => P.low),
                                    high = g.Max(P => P.high),
                                    type = type,
                                    time_start = FactoryService.instance.system_init.AddMinutes(g.Key * 15),
                                    time_end = FactoryService.instance.system_init.AddMinutes((g.Key + 1) * 15).AddMilliseconds(-1),
                                    time = DateTimeOffset.UtcNow,
                                };
                    return sql15.ToList();
                case E_KlineType.min30:
                    predicate = predicate.And(P => P.type == E_KlineType.min15);
                    var sql30 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                orderby kline.time_start
                                group kline by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, kline.time_start) / 30 into g
                                select new Kline
                                {
                                    market = market,
                                    amount = g.Sum(P => P.amount),
                                    count = g.Sum(P => P.count),
                                    total = g.Sum(P => P.total),
                                    open = g.OrderBy(P => P.time_start).First().open,
                                    close = g.OrderBy(P => P.time_start).Last().close,
                                    low = g.Min(P => P.low),
                                    high = g.Max(P => P.high),
                                    type = type,
                                    time_start = FactoryService.instance.system_init.AddMinutes(g.Key * 30),
                                    time_end = FactoryService.instance.system_init.AddMinutes((g.Key + 1) * 30).AddMilliseconds(-1),
                                    time = DateTimeOffset.UtcNow,
                                };
                    return sql30.ToList();
                case E_KlineType.hour1:
                    predicate = predicate.And(P => P.type == E_KlineType.min30);
                    var sqlhour1 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                   orderby kline.time_start
                                   group kline by EF.Functions.DateDiffHour(FactoryService.instance.system_init, kline.time_start) into g
                                   select new Kline
                                   {
                                       market = market,
                                       amount = g.Sum(P => P.amount),
                                       count = g.Sum(P => P.count),
                                       total = g.Sum(P => P.total),
                                       open = g.OrderBy(P => P.time_start).First().open,
                                       close = g.OrderBy(P => P.time_start).Last().close,
                                       low = g.Min(P => P.low),
                                       high = g.Max(P => P.high),
                                       type = type,
                                       time_start = FactoryService.instance.system_init.AddHours(g.Key),
                                       time_end = FactoryService.instance.system_init.AddHours(g.Key + 1).AddMilliseconds(-1),
                                       time = DateTimeOffset.UtcNow,
                                   };
                    return sqlhour1.ToList();
                case E_KlineType.hour6:
                    predicate = predicate.And(P => P.type == E_KlineType.hour1);
                    var sqlhour6 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                   orderby kline.time_start
                                   group kline by EF.Functions.DateDiffHour(FactoryService.instance.system_init, kline.time_start) / 6 into g
                                   select new Kline
                                   {
                                       market = market,
                                       amount = g.Sum(P => P.amount),
                                       count = g.Sum(P => P.count),
                                       total = g.Sum(P => P.total),
                                       open = g.OrderBy(P => P.time_start).First().open,
                                       close = g.OrderBy(P => P.time_start).Last().close,
                                       low = g.Min(P => P.low),
                                       high = g.Max(P => P.high),
                                       type = type,
                                       time_start = FactoryService.instance.system_init.AddHours(g.Key * 6),
                                       time_end = FactoryService.instance.system_init.AddHours((g.Key + 1) * 6).AddMilliseconds(-1),
                                       time = DateTimeOffset.UtcNow,
                                   };
                    return sqlhour6.ToList();
                case E_KlineType.hour12:
                    predicate = predicate.And(P => P.type == E_KlineType.hour6);
                    var sqlhour12 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                    orderby kline.time_start
                                    group kline by EF.Functions.DateDiffHour(FactoryService.instance.system_init, kline.time_start) / 12 into g
                                    select new Kline
                                    {
                                        market = market,
                                        amount = g.Sum(P => P.amount),
                                        count = g.Sum(P => P.count),
                                        total = g.Sum(P => P.total),
                                        open = g.OrderBy(P => P.time_start).First().open,
                                        close = g.OrderBy(P => P.time_start).Last().close,
                                        low = g.Min(P => P.low),
                                        high = g.Max(P => P.high),
                                        type = type,
                                        time_start = FactoryService.instance.system_init.AddHours(g.Key * 12),
                                        time_end = FactoryService.instance.system_init.AddHours((g.Key + 1) * 12).AddMilliseconds(-1),
                                        time = DateTimeOffset.UtcNow,
                                    };
                    return sqlhour12.ToList();
                case E_KlineType.day1:
                    predicate = predicate.And(P => P.type == E_KlineType.hour12);
                    var sqlday1 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                  orderby kline.time_start
                                  group kline by EF.Functions.DateDiffDay(FactoryService.instance.system_init, kline.time_start) into g
                                  select new Kline
                                  {
                                      market = market,
                                      amount = g.Sum(P => P.amount),
                                      count = g.Sum(P => P.count),
                                      total = g.Sum(P => P.total),
                                      open = g.OrderBy(P => P.time_start).First().open,
                                      close = g.OrderBy(P => P.time_start).Last().close,
                                      low = g.Min(P => P.low),
                                      high = g.Max(P => P.high),
                                      type = type,
                                      time_start = FactoryService.instance.system_init.AddDays(g.Key),
                                      time_end = FactoryService.instance.system_init.AddDays(g.Key + 1).AddMilliseconds(-1),
                                      time = DateTimeOffset.UtcNow,
                                  };
                    return sqlday1.ToList();
                case E_KlineType.week1:
                    predicate = predicate.And(P => P.type == E_KlineType.day1);
                    var sqlweek1 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                   orderby kline.time_start
                                   group kline by EF.Functions.DateDiffWeek(FactoryService.instance.system_init, kline.time_start) into g
                                   select new Kline
                                   {
                                       market = market,
                                       amount = g.Sum(P => P.amount),
                                       count = g.Sum(P => P.count),
                                       total = g.Sum(P => P.total),
                                       open = g.OrderBy(P => P.time_start).First().open,
                                       close = g.OrderBy(P => P.time_start).Last().close,
                                       low = g.Min(P => P.low),
                                       high = g.Max(P => P.high),
                                       type = type,
                                       time_start = FactoryService.instance.system_init.AddDays(g.Key * 7),
                                       time_end = FactoryService.instance.system_init.AddDays((g.Key + 1) * 7).AddMilliseconds(-1),
                                       time = DateTimeOffset.UtcNow,
                                   };
                    return sqlweek1.ToList();
                case E_KlineType.month1:
                    predicate = predicate.And(P => P.type == E_KlineType.day1);
                    var sqlmonth1 = from kline in FactoryService.instance.constant.db.Kline.Where(predicate)
                                    orderby kline.time_start
                                    group kline by EF.Functions.DateDiffMonth(FactoryService.instance.system_init, kline.time_start) into g
                                    select new Kline
                                    {
                                        market = market,
                                        amount = g.Sum(P => P.amount),
                                        count = g.Sum(P => P.count),
                                        total = g.Sum(P => P.total),
                                        open = g.OrderBy(P => P.time_start).First().open,
                                        close = g.OrderBy(P => P.time_start).Last().close,
                                        low = g.Min(P => P.low),
                                        high = g.Max(P => P.high),
                                        type = type,
                                        time_start = FactoryService.instance.system_init.AddMonths(g.Key),
                                        time_end = FactoryService.instance.system_init.AddMonths(g.Key + 1).AddMilliseconds(-1),
                                        time = DateTimeOffset.UtcNow,
                                    };
                    return sqlmonth1.ToList();
            }
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

}