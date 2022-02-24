﻿using Com.Common;
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
        DateTimeOffset start = DateTimeOffset.MinValue;
        if (last_kline != null)
        {
            start = last_kline.time_start.Add(span);
        }
        List<Deal> deals = this.constant.db.Deal.Where(P => P.market == market && start <= P.time && P.time < end).OrderBy(P => P.time).ToList();
        // foreach (var item in deals)
        // {
        //     item.timestamp = item.time.ToUnixTimeMilliseconds();
        // }
        // this.constant.db.SaveChanges();
        // this.constant.db.Remove(deals[0]);
        // this.constant.db.SaveChanges();

        for (DateTimeOffset i = start; i <= end; i.Add(span))
        {

        }
        return result;
    }

    /// <summary>
    /// 从数据库统计K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<BaseKline> GetKlines1(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end, TimeSpan span)
    {
        List<BaseKline> result = new List<BaseKline>();
        int minutes = 0;
        switch (klineType)
        {
            case E_KlineType.min1:
                minutes = 1;
                break;
            case E_KlineType.min5:
                minutes = 5;
                break;
            case E_KlineType.min15:
                minutes = 15;
                break;
            case E_KlineType.min30:
                minutes = 30;
                break;
            case E_KlineType.hour1:
                minutes = 60;
                break;
            case E_KlineType.hour4:
                minutes = 240;
                break;
            case E_KlineType.day1:
                minutes = 1440;
                break;
            default:
                break;
        }
        // DateTimeOffset init = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // var bbbb = this.constant.db.Deal.OrderBy(P => P.time).Where(P => P.market == market && P.time < end).ToList();
        // var aaa = this.constant.db.Set<Deal>().ToList();
        // var ccc = this.constant.db.Set<Deal>().OrderBy(P => P.time).Where(P => P.market == market).ToList();

        // return result;
        if (minutes > 0)
        {
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
        }
        // else if (klineType == E_KlineType.week1)
        // {

        //     var sql = from deal in this.constant.db.Deal.OrderBy(P => P.timestamp).Where(P => P.market == market && start <= P.time && P.time < end)
        //               group deal by EF.Functions.DateDiffWeek(init, deal.time) into g
        //               select new BaseKline
        //               {
        //                   market = market,
        //                   amount = g.Sum(P => P.amount),
        //                   count = g.Count(),
        //                   total = g.Sum(P => P.price * P.amount),
        //                   open = g.First().price,
        //                   close = g.Last().price,
        //                   low = g.Min(P => P.price),
        //                   high = g.Max(P => P.price),
        //                   type = klineType,
        //                   time_start = DateTimeOffset.FromUnixTimeSeconds(g.Key * 7 * 24 * 60 * 60),
        //                   time_end = DateTimeOffset.FromUnixTimeSeconds(g.Key * 13 * 24 * 60 * 60).AddMinutes(minutes),
        //                   time = DateTimeOffset.UtcNow,
        //               };
        //     result = sql.ToList();
        // }
        // else if (klineType == E_KlineType.month1)
        // {

        //     var sql = from deal in this.constant.db.Deal.OrderBy(P => P.timestamp).Where(P => P.market == market && start <= P.time && P.time < end)
        //               group deal by EF.Functions.DateDiffMonth(init, deal.time) into g
        //               select new BaseKline
        //               {
        //                   market = market,
        //                   amount = g.Sum(P => P.amount),
        //                   count = g.Count(),
        //                   total = g.Sum(P => P.price * P.amount),
        //                   open = g.First().price,
        //                   close = g.Last().price,
        //                   low = g.Min(P => P.price),
        //                   high = g.Max(P => P.price),
        //                   type = klineType,
        //                   time_start = DateTimeOffset.FromUnixTimeSeconds(g.Key * 7 * 24 * 60 * 60),
        //                   time_end = DateTimeOffset.FromUnixTimeSeconds(g.Key * 13 * 24 * 60 * 60).AddMinutes(minutes),
        //                   time = DateTimeOffset.UtcNow,
        //               };
        //     result = sql.ToList();


        return result;
    }


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