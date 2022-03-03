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
public class KilneHelper
{

    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="constant"></param>
    public KilneHelper(FactoryConstant constant)
    {
        this.constant = constant;
    }

    /// <summary>
    /// 获取最后一条K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <returns></returns>
    public Kline? GetLastKline(string market, E_KlineType klineType)
    {
        return this.constant.db.Kline.Where(P => P.market == market && P.type == klineType).OrderByDescending(P => P.time_start).FirstOrDefault();
    }


    /// <summary>
    /// Deal 转1分钟K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="end"></param>
    /// <param name="last_kline"></param>
    /// <returns></returns>
    public List<Kline> GetKlineMin(string market, DateTimeOffset end, Kline? last_kline)
    {
        List<Kline> result = new List<Kline>();
        DateTimeOffset start = KlineService.instance.system_init;
        decimal last_price = 0;
        if (last_kline != null)
        {
            last_price = last_kline.close;
            start = last_kline.time_end.AddMilliseconds(1);
        }
        List<Deal> deals = DealService.instance.dealHelper.GetDeals(market, start, end);
        if (last_kline == null && deals.Count > 0)
        {
            DateTimeOffset first_time = deals.First().time;
            start = first_time.AddSeconds(-first_time.Second).AddMilliseconds(-first_time.Millisecond);
        }
        for (DateTimeOffset i = start; i <= end; i = i.AddMinutes(1))
        {
            DateTimeOffset end_time = i.AddMinutes(1).AddMilliseconds(-1);
            List<Deal> deal = deals.Where(P => P.time >= i && P.time <= end_time).ToList();
            if (deal.Count > 0)
            {
                last_price = deal.Last().price;
            }
            if (last_price == 0 && deal.Count == 0)
            {
                continue;
            }
            Kline? kline = DealToKline(market, E_KlineType.min1, i, end_time, last_price, deal);
            if (kline != null)
            {
                result.Add(kline);
            }
        }
        return result;
    }


    /// <summary>
    /// 从数据库Deal统计K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Kline> GetKlines(string market, E_KlineType klineType, Kline? last_kline, DateTimeOffset end, TimeSpan span)
    {
        List<Kline> result = new List<Kline>();
        DateTimeOffset start = KlineService.instance.system_init;
        decimal last_price = 0;
        if (last_kline != null)
        {
            start = last_kline.time_start.Add(span);
            last_price = last_kline.close;
        }
        List<Deal> deals = this.constant.db.Deal.Where(P => P.market == market && start <= P.time && P.time <= end).OrderBy(P => P.time).ToList();
        if (last_price == 0 && deals.Count == 0)
        {
            return result;
        }
        else if (deals.Count > 0)
        {
            start = deals.First().time;
            start = start.AddSeconds(-start.Second).AddMilliseconds(-start.Millisecond);
        }
        for (DateTimeOffset i = start; i <= end; i = i.Add(span))
        {
            DateTimeOffset end_time = i.Add(span).AddMilliseconds(-1);
            List<Deal> deal = deals.Where(P => P.time >= i && P.time <= end_time).ToList();
            if (last_price == 0 && deal.Count == 0)
            {
                continue;
            }
            Kline? kline = DealToKline(market, klineType, i, end_time, last_price, deal);
            if (kline != null)
            {
                result.Add(kline);
                last_price = kline.close;
            }
        }
        return result;
    }

    /// <summary>
    /// 交易记录转换成K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="last_price"></param>
    /// <param name="deals"></param>
    /// <returns></returns>
    public Kline? DealToKline(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end, decimal last_price, List<Deal> deals)
    {
        Kline kline = new Kline();
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

    /// <summary>
    /// 保存K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="klines"></param>
    /// <returns></returns>
    public int SaveKline(string market, E_KlineType klineType, List<Kline> klines)
    {
        if (klines == null || klines.Count == 0)
        {
            return 0;
        }
        List<Kline> db_kline = this.constant.db.Kline.Where(P => P.market == market && P.type == klineType && P.time_start >= klines[0].time_start && P.time_end <= klines[klines.Count - 1].time_end).ToList();
        foreach (var item in klines)
        {
            Kline? kline = db_kline.FirstOrDefault(P => P.time_start == item.time_start);
            if (kline == null)
            {
                kline = new Kline();
                kline.id = this.constant.worker.NextId();
                kline.time_start = item.time_start;
                kline.time_end = item.time_end;
                kline.time = item.time;
                this.constant.db.Kline.Add(kline);
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
        return this.constant.db.SaveChanges();
    }


    /// <summary>
    /// 从数据库统计K线(除1分钟K线外)
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType_source"></param>
    /// <param name="klineType_target"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Kline>? CalcKlines(string market, E_KlineType type, DateTimeOffset? start, DateTimeOffset? end)
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
                    return DealService.instance.dealHelper.GetKlinesMin1ByDeal(market, start, end);
                case E_KlineType.min5:
                    predicate = predicate.And(P => P.type == E_KlineType.min1);
                    var sql5 = from kline in this.constant.db.Kline.Where(predicate)
                               orderby kline.time_start
                               group kline by EF.Functions.DateDiffMinute(KlineService.instance.system_init, kline.time_start) / 5 into g
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
                                   time_start = KlineService.instance.system_init.AddMinutes(g.Key * 5),
                                   time_end = KlineService.instance.system_init.AddMinutes((g.Key + 1) * 5).AddMilliseconds(-1),
                                   time = DateTimeOffset.UtcNow,
                               };
                    return sql5.ToList();
                case E_KlineType.min15:
                    predicate = predicate.And(P => P.type == E_KlineType.min5);
                    var sql15 = from kline in this.constant.db.Kline.Where(predicate)
                                orderby kline.time_start
                                group kline by EF.Functions.DateDiffMinute(KlineService.instance.system_init, kline.time_start) / 15 into g
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
                                    time_start = KlineService.instance.system_init.AddMinutes(g.Key * 15),
                                    time_end = KlineService.instance.system_init.AddMinutes((g.Key + 1) * 15).AddMilliseconds(-1),
                                    time = DateTimeOffset.UtcNow,
                                };
                    return sql15.ToList();
                case E_KlineType.min30:
                    predicate = predicate.And(P => P.type == E_KlineType.min15);
                    var sql30 = from kline in this.constant.db.Kline.Where(predicate)
                                orderby kline.time_start
                                group kline by EF.Functions.DateDiffMinute(KlineService.instance.system_init, kline.time_start) / 30 into g
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
                                    time_start = KlineService.instance.system_init.AddMinutes(g.Key * 30),
                                    time_end = KlineService.instance.system_init.AddMinutes((g.Key + 1) * 30).AddMilliseconds(-1),
                                    time = DateTimeOffset.UtcNow,
                                };
                    return sql30.ToList();
                case E_KlineType.hour1:
                    predicate = predicate.And(P => P.type == E_KlineType.min30);
                    var sqlhour1 = from kline in this.constant.db.Kline.Where(predicate)
                                   orderby kline.time_start
                                   group kline by EF.Functions.DateDiffHour(KlineService.instance.system_init, kline.time_start) into g
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
                                       time_start = KlineService.instance.system_init.AddHours(g.Key),
                                       time_end = KlineService.instance.system_init.AddHours(g.Key + 1).AddMilliseconds(-1),
                                       time = DateTimeOffset.UtcNow,
                                   };
                    return sqlhour1.ToList();
                case E_KlineType.hour6:
                    predicate = predicate.And(P => P.type == E_KlineType.hour1);
                    var sqlhour6 = from kline in this.constant.db.Kline.Where(predicate)
                                   orderby kline.time_start
                                   group kline by EF.Functions.DateDiffHour(KlineService.instance.system_init, kline.time_start) / 6 into g
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
                                       time_start = KlineService.instance.system_init.AddHours(g.Key * 6),
                                       time_end = KlineService.instance.system_init.AddHours((g.Key + 1) * 6).AddMilliseconds(-1),
                                       time = DateTimeOffset.UtcNow,
                                   };
                    return sqlhour6.ToList();
                case E_KlineType.hour12:
                    predicate = predicate.And(P => P.type == E_KlineType.hour6);
                    var sqlhour12 = from kline in this.constant.db.Kline.Where(predicate)
                                    orderby kline.time_start
                                    group kline by EF.Functions.DateDiffHour(KlineService.instance.system_init, kline.time_start) / 12 into g
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
                                        time_start = KlineService.instance.system_init.AddHours(g.Key * 12),
                                        time_end = KlineService.instance.system_init.AddHours((g.Key + 1) * 12).AddMilliseconds(-1),
                                        time = DateTimeOffset.UtcNow,
                                    };
                    return sqlhour12.ToList();
                case E_KlineType.day1:
                    predicate = predicate.And(P => P.type == E_KlineType.hour12);
                    var sqlday1 = from kline in this.constant.db.Kline.Where(predicate)
                                  orderby kline.time_start
                                  group kline by EF.Functions.DateDiffDay(KlineService.instance.system_init, kline.time_start) into g
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
                                      time_start = KlineService.instance.system_init.AddDays(g.Key),
                                      time_end = KlineService.instance.system_init.AddDays(g.Key + 1).AddMilliseconds(-1),
                                      time = DateTimeOffset.UtcNow,
                                  };
                    return sqlday1.ToList();
                case E_KlineType.week1:
                    predicate = predicate.And(P => P.type == E_KlineType.day1);
                    var sqlweek1 = from kline in this.constant.db.Kline.Where(predicate)
                                   orderby kline.time_start
                                   group kline by EF.Functions.DateDiffWeek(KlineService.instance.system_init, kline.time_start) into g
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
                                       time_start = KlineService.instance.system_init.AddDays(g.Key * 7),
                                       time_end = KlineService.instance.system_init.AddDays((g.Key + 1) * 7).AddMilliseconds(-1),
                                       time = DateTimeOffset.UtcNow,
                                   };
                    return sqlweek1.ToList();
                case E_KlineType.month1:
                    predicate = predicate.And(P => P.type == E_KlineType.day1);
                    var sqlmonth1 = from kline in this.constant.db.Kline.Where(predicate)
                                    orderby kline.time_start
                                    group kline by EF.Functions.DateDiffMonth(KlineService.instance.system_init, kline.time_start) into g
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
                                        time_start = KlineService.instance.system_init.AddMonths(g.Key),
                                        time_end = KlineService.instance.system_init.AddMonths(g.Key + 1).AddMilliseconds(-1),
                                        time = DateTimeOffset.UtcNow,
                                    };
                    return sqlmonth1.ToList();
            }
        }
        catch (System.Exception ex)
        {
            this.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;


        // List<Kline> result = new List<Kline>();
        // Expression<Func<Kline, int>> lambda = P => EF.Functions.DateDiffMinute(KlineService.instance.system_init, P.time_start);
        // switch (klineType_target)
        // {
        //     case E_KlineType.min1:
        //         lambda = P => EF.Functions.DateDiffMinute(KlineService.instance.system_init, P.time_start);
        //         break;
        //     case E_KlineType.min5:
        //         lambda = P => EF.Functions.DateDiffMinute(KlineService.instance.system_init, P.time_start) / 5;
        //         break;
        //     case E_KlineType.min15:
        //         lambda = P => EF.Functions.DateDiffMinute(KlineService.instance.system_init, P.time_start) / 15;
        //         break;
        //     case E_KlineType.min30:
        //         lambda = P => EF.Functions.DateDiffMinute(KlineService.instance.system_init, P.time_start) / 30;
        //         break;
        //     case E_KlineType.hour1:
        //         lambda = P => EF.Functions.DateDiffHour(KlineService.instance.system_init, P.time_start);
        //         break;
        //     case E_KlineType.hour12:
        //         lambda = P => EF.Functions.DateDiffHour(KlineService.instance.system_init, P.time_start) / 12;
        //         break;
        //     case E_KlineType.day1:
        //         lambda = P => EF.Functions.DateDiffDay(KlineService.instance.system_init, P.time_start);
        //         break;
        //     case E_KlineType.week1:
        //         lambda = P => EF.Functions.DateDiffWeek(KlineService.instance.system_init, P.time_start);
        //         break;
        //     case E_KlineType.month1:
        //         lambda = P => EF.Functions.DateDiffMonth(KlineService.instance.system_init, P.time_start);
        //         break;
        //     default:
        //         lambda = P => EF.Functions.DateDiffMinute(KlineService.instance.system_init, P.time_start);
        //         break;
        // }
        // // var sql = from kline in this.constant.db.Kline
        // //           where kline.market == market && kline.type == klineType_source && start <= kline.time && kline.time <= end
        // //           orderby kline.time_start
        // //           group kline by lambda into g
        // //           select new Kline
        // //           {
        // //               market = market,
        // //               amount = g.Sum(P => P.amount),
        // //               count = g.Count(),
        // //               total = g.Sum(P => P.total),
        // //               open = g.First().open,
        // //               close = g.Last().close,
        // //               low = g.Min(P => P.low),
        // //               high = g.Max(P => P.high),
        // //               type = klineType_target,
        // //               time_start = g.First().time_start,
        // //               time_end = g.Last().time_end,
        // //               time = DateTimeOffset.UtcNow,
        // //           };

        // List<Kline> klines1 = this.constant.db.Kline.ToList();
        // List<Kline> klines2 = this.constant.db.Kline.Where(P => P.time_start >= start && P.time_start <= end).ToList();
        // List<Kline> klines = this.constant.db.Kline.Where(P => P.market == market && P.type == klineType_source && P.time_start >= start && P.time_start <= end).OrderBy(P => P.time_start).ToList();

        // var sql = from kline in this.constant.db.Set<Kline>()
        //               //   where kline.market == market && kline.type == klineType_source && end >= kline.time && end > kline.time
        //           orderby kline.open
        //           //   group kline by lambda into g
        //           group kline by EF.Functions.DateDiffMinute(KlineService.instance.system_init, kline.time_end) into g
        //           //   group kline by kline.open into g
        //           //   select new
        //           //   {
        //           //       time_end = g.Key,
        //           //       //   amount = g.Sum(P => P.amount),
        //           //   };
        //           select new Kline
        //           {
        //               //   open = g.Key,
        //               market = market,
        //               amount = g.Sum(P => P.amount),
        //               count = g.Count(),
        //               total = g.Sum(P => P.total),
        //               open = g.First().open,
        //               close = g.Last().close,
        //               low = g.Min(P => P.low),
        //               high = g.Max(P => P.high),
        //               type = klineType_target,
        //               time_start = g.First().time_start,
        //               time_end = g.Last().time_end,
        //               time = DateTimeOffset.UtcNow,
        //           };
        // try
        // {
        //     var a = sql.ToList();
        //     // result = sql.ToList();
        // }
        // catch (System.Exception ex)
        // {

        //     throw;
        // }
        // return result;
    }

    /// <summary>
    /// 从数据库获取K线数据
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Kline> GetKlines(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end)
    {
        return this.constant.db.Kline.Where(P => P.market == market && P.type == klineType && P.time_start >= start && P.time_start <= end).OrderBy(P => P.time_start).ToList();
    }



}