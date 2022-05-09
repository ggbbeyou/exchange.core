using Com.Db;
using Com.Api.Sdk.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using Com.Bll.Util;

namespace Com.Bll;

/// <summary>
/// Service:K线
/// </summary>
public class ServiceKline
{
    /// <summary>
    /// DB:交易记录
    /// </summary>
    private ServiceDeal service_deal = new ServiceDeal();

    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceKline()
    {
    }

    /// <summary>
    /// db获取最后一条K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="type">K线类型</param>
    /// <returns></returns>
    public Kline? GetLastKline(long market, E_KlineType type)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.Kline.AsNoTracking().Where(P => P.market == market && P.type == type).OrderByDescending(P => P.time_start).FirstOrDefault();
            }
        }
    }

    /*
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
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    return db.Kline.AsNoTracking().Where(P => P.market == market && P.type == type && P.time_start >= start && P.time_start <= end).OrderBy(P => P.time_start).ToList();
                }
            }
        }

        */

    /*
        /// <summary>
        /// 保存K线
        /// </summary>
        /// <param name="market">交易对</param>
        /// <param name="type">K线类型</param>
        /// <param name="klines">K线集合</param>
        /// <returns></returns>
        public int SaveKline(long market, string symbol, E_KlineType type, List<Kline> klines)
        {
            if (klines == null || klines.Count == 0)
            {
                return 0;
            }
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    List<Kline> db_kline = db.Kline.Where(P => P.market == market && P.type == type && P.time_start >= klines[0].time_start && P.time_end <= klines[klines.Count - 1].time_end).ToList();
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
                            db.Kline.Add(kline);
                        }
                        kline.market = market;
                        kline.symbol = symbol;
                        kline.type = type;
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
                    db.Kline.AddRange(klines);
                    return db.SaveChanges();
                }
            }
        }        
    */

    /// <summary>
    /// 保存k线记录
    /// </summary>
    /// <param name="klines">集合</param>
    /// <returns></returns>
    public int SaveKline(List<Kline> klines)
    {
        if (klines == null || klines.Count == 0)
        {
            return 0;
        }
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                db.Kline.AddRange(klines);
                return db.SaveChanges();
            }
        }
    }

    /// <summary>
    ///  计算出K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="type">K线类型</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Kline>? CalcKlines(long market, string symbol, E_KlineType type, DateTimeOffset? start, DateTimeOffset? end)
    {
        try
        {
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    switch (type)
                    {
                        case E_KlineType.min1:
                            return this.service_deal.GetKlinesMin1ByDeal(market, symbol, start, end);
                        case E_KlineType.min5:
                            var sql5 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.min1).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                       orderby kline.time_start
                                       group kline by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, kline.time_start) / 5 into g
                                       select new Kline
                                       {
                                           id = FactoryService.instance.constant.worker.NextId(),
                                           market = market,
                                           symbol = symbol,
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
                            return sql5.AsNoTracking().ToList();
                        case E_KlineType.min15:
                            var sql15 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.min5).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                        orderby kline.time_start
                                        group kline by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, kline.time_start) / 15 into g
                                        select new Kline
                                        {
                                            id = FactoryService.instance.constant.worker.NextId(),
                                            market = market,
                                            symbol = symbol,
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
                            return sql15.AsNoTracking().ToList();
                        case E_KlineType.min30:
                            var sql30 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.min15).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                        orderby kline.time_start
                                        group kline by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, kline.time_start) / 30 into g
                                        select new Kline
                                        {
                                            id = FactoryService.instance.constant.worker.NextId(),
                                            market = market,
                                            symbol = symbol,
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
                            return sql30.AsNoTracking().ToList();
                        case E_KlineType.hour1:
                            var sqlhour1 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.min30).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                           orderby kline.time_start
                                           group kline by EF.Functions.DateDiffHour(FactoryService.instance.system_init, kline.time_start) into g
                                           select new Kline
                                           {
                                               id = FactoryService.instance.constant.worker.NextId(),
                                               market = market,
                                               symbol = symbol,
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
                            return sqlhour1.AsNoTracking().ToList();
                        case E_KlineType.hour6:
                            var sqlhour6 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.hour1).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                           orderby kline.time_start
                                           group kline by EF.Functions.DateDiffHour(FactoryService.instance.system_init, kline.time_start) / 6 into g
                                           select new Kline
                                           {
                                               id = FactoryService.instance.constant.worker.NextId(),
                                               market = market,
                                               symbol = symbol,
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
                            return sqlhour6.AsNoTracking().ToList();
                        case E_KlineType.hour12:
                            var sqlhour12 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.hour6).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                            orderby kline.time_start
                                            group kline by EF.Functions.DateDiffHour(FactoryService.instance.system_init, kline.time_start) / 12 into g
                                            select new Kline
                                            {
                                                id = FactoryService.instance.constant.worker.NextId(),
                                                market = market,
                                                symbol = symbol,
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
                            return sqlhour12.AsNoTracking().ToList();
                        case E_KlineType.day1:
                            var sqlday1 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.hour12).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                          orderby kline.time_start
                                          group kline by EF.Functions.DateDiffDay(FactoryService.instance.system_init, kline.time_start) into g
                                          select new Kline
                                          {
                                              id = FactoryService.instance.constant.worker.NextId(),
                                              market = market,
                                              symbol = symbol,
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
                            return sqlday1.AsNoTracking().ToList();
                        case E_KlineType.week1:
                            var sqlweek1 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.day1).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                           orderby kline.time_start
                                           group kline by EF.Functions.DateDiffWeek(FactoryService.instance.system_init, kline.time_start) into g
                                           select new Kline
                                           {
                                               id = FactoryService.instance.constant.worker.NextId(),
                                               market = market,
                                               symbol = symbol,
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
                            return sqlweek1.AsNoTracking().ToList();
                        case E_KlineType.month1:
                            var sqlmonth1 = from kline in db.Kline.Where(P => P.market == market && P.type == E_KlineType.day1).WhereIf(start != null, P => start <= P.time_start).WhereIf(end != null, P => P.time_end <= end)
                                            orderby kline.time_start
                                            group kline by EF.Functions.DateDiffMonth(FactoryService.instance.system_init, kline.time_start) into g
                                            select new Kline
                                            {
                                                id = FactoryService.instance.constant.worker.NextId(),
                                                market = market,
                                                symbol = symbol,
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
                            return sqlmonth1.AsNoTracking().ToList();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region 已确定K线

    /// <summary>
    /// 缓存预热(已确定K线)
    /// </summary>
    /// <param name="markets">交易对</param>
    /// <param name="end">结束时间</param>
    public Dictionary<E_KlineType, List<Kline>> DBtoRedised(long market, string symbol, DateTimeOffset end)
    {
        Dictionary<E_KlineType, List<Kline>> klines = SyncKlines(market, symbol, end);
        DbSaveRedis(market, klines);
        return klines;
    }

    /// <summary>
    /// 将K线保存到Db中
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public Dictionary<E_KlineType, List<Kline>> SyncKlines(long market, string symbol, DateTimeOffset end)
    {
        Dictionary<E_KlineType, List<Kline>> klines = new Dictionary<E_KlineType, List<Kline>>();
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            DateTimeOffset start = FactoryService.instance.system_init;
            Kline? last_kline = this.GetLastKline(market, cycle);
            if (last_kline != null)
            {
                start = last_kline.time_end.AddMilliseconds(1);
            }
            List<Kline>? kline = this.CalcKlines(market, symbol, cycle, start, end);
            if (kline != null && kline.Count > 0)
            {
                int count = this.SaveKline(kline);
                klines.Add(cycle, kline);
            }
        }
        return klines;
    }

    /*
        /// <summary>
        /// 将DB中的K线数据保存到Redis
        /// </summary>
        /// <param name="market">交易对</param>
        /// <param name="end">结束时间</param>
        public void DbSaveRedis(long market, DateTimeOffset end)
        {
            foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
            {
                Kline? Last_kline = GetRedisLastKline(market, cycle);
                List<Kline> klines = this.GetKlines(market, cycle, Last_kline?.time_end ?? FactoryService.instance.system_init, end);
                if (klines.Count() > 0)
                {
                    SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
                    for (int i = 0; i < klines.Count(); i++)
                    {
                        entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i], new JsonConverterDecimal()), klines[i].time_start.ToUnixTimeMilliseconds());
                    }
                    FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisKline(market, cycle), entries);
                }
            }
        }
        */

    /// <summary>
    /// 将DB中的K线数据保存到Redis
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="end">结束时间</param>
    public void DbSaveRedis(long market, Dictionary<E_KlineType, List<Kline>> klines)
    {
        foreach (var item in klines)
        {
            DateTimeOffset start = FactoryService.instance.system_init;
            Kline? Last_kline = GetRedisLastKline(market, item.Key);
            if (Last_kline != null)
            {
                start = Last_kline.time_end.AddMilliseconds(1);
            }
            List<Kline> kline = item.Value.Where(P => P.time_start >= start).ToList();
            if (kline.Count() > 0)
            {
                SortedSetEntry[] entries = new SortedSetEntry[kline.Count()];
                for (int i = 0; i < kline.Count(); i++)
                {
                    entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(kline[i], new JsonConverterDecimal()), kline[i].time_start.ToUnixTimeMilliseconds());
                }
                FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisKline(market, item.Key), entries);
            }
        }
    }

    /// <summary>
    /// 从redis获取最大的K线时间
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="klineType">K线类型</param>
    /// <returns></returns>
    public Kline? GetRedisLastKline(long market, E_KlineType klineType)
    {
        RedisValue[] redisvalue = FactoryService.instance.constant.redis.SortedSetRangeByRank(FactoryService.instance.GetRedisKline(market, klineType), 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return JsonConvert.DeserializeObject<Kline>(redisvalue[0]);
        }
        return null;
    }

    /*
        /// <summary>
        /// 从redis获取的K线
        /// </summary>
        /// <param name="market">交易对</param>
        /// <param name="klineType">K线类型</param>
        /// <returns></returns>
        public List<Kline> GetRedisKline(long market, E_KlineType klineType, DateTimeOffset? start, DateTimeOffset? end)
        {
            List<Kline> klines = new List<Kline>();
            double start1 = double.NegativeInfinity;
            double stop1 = double.PositiveInfinity;
            if (start != null)
            {
                start1 = start.Value.ToUnixTimeMilliseconds();
            }
            if (end != null)
            {
                stop1 = end.Value.ToUnixTimeMilliseconds();
            }
            RedisValue[] redisvalue = FactoryService.instance.constant.redis.SortedSetRangeByScore(key: FactoryService.instance.GetRedisKline(market, klineType), start: start1, stop: stop1, order: StackExchange.Redis.Order.Ascending);
            foreach (var item in redisvalue)
            {
                klines.Add(JsonConvert.DeserializeObject<Kline>(item)!);
            }
            return klines;
        }
        */

    #endregion


    #region 未确定K线

    /// <summary>
    /// 缓存预热(未确定K线)
    /// </summary>
    /// <param name="market">交易对</param>
    public void DBtoRedising(long market, string symbol, DateTimeOffset now)
    {
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            (DateTimeOffset start, DateTimeOffset end) startend = KlineTime(cycle, now);
            Kline? kline_new = this.service_deal.GetKlinesByDeal(market, cycle, startend.start, startend.end);
            if (kline_new == null)
            {
                Deal? last_deal = service_deal.GetRedisLastDeal(market);
                if (last_deal == null)
                {
                    FactoryService.instance.constant.redis.HashDelete(FactoryService.instance.GetRedisKlineing(market), cycle.ToString());
                }
                else
                {
                    kline_new = new Kline()
                    {
                        market = market,
                        symbol = symbol,
                        amount = 0,
                        count = 0,
                        total = 0,
                        open = last_deal.price,
                        close = last_deal.price,
                        low = last_deal.price,
                        high = last_deal.price,
                        type = cycle,
                        time_start = startend.start,
                        time_end = now,
                        time = now,
                    };
                    FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisKlineing(market), cycle.ToString(), JsonConvert.SerializeObject(kline_new, new JsonConverterDecimal()));
                }
            }
            else
            {
                FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisKlineing(market), cycle.ToString(), JsonConvert.SerializeObject(kline_new, new JsonConverterDecimal()));
            }
        }
    }

    /// <summary>
    /// 未确定K线和交易记录合并成新的K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="symbol"></param>
    /// <param name="deals"></param>
    public Dictionary<E_KlineType, Kline> DBtoRedising(long market, string symbol, List<Deal> deals)
    {
        Dictionary<E_KlineType, Kline> klines = new Dictionary<E_KlineType, Kline>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            (DateTimeOffset start, DateTimeOffset end) startend = KlineTime(cycle, now);
            List<Deal> deals_cycle = deals.Where(x => x.time >= startend.start && x.time <= startend.end).ToList();
            Kline? kline_new = DealToKline(cycle, startend.start, deals_cycle);
            if (kline_new == null)
            {
                continue;
            }
            RedisValue kline_old_obj = FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisKlineing(market), cycle.ToString());
            if (kline_old_obj.HasValue)
            {
                Kline? kline_old = JsonConvert.DeserializeObject<Kline>(kline_old_obj);
                if (kline_old != null && kline_old.time_start == kline_new.time_start)
                {
                    kline_new.id = FactoryService.instance.constant.worker.NextId();
                    kline_new.amount += kline_old.amount;
                    kline_new.count += kline_old.count;
                    kline_new.total += kline_old.total;
                    kline_new.open = kline_old.open;
                    kline_new.close = kline_new.close;
                    kline_new.low = kline_new.low > kline_old.low ? kline_old.low : kline_new.low;
                    kline_new.high = kline_new.high < kline_old.high ? kline_old.high : kline_new.high;
                    kline_new.time_start = kline_old.time_start;
                }
            }
            FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisKlineing(market), cycle.ToString(), JsonConvert.SerializeObject(kline_new, new JsonConverterDecimal()));
            klines.Add(cycle, kline_new);
        }
        return klines;
    }

    #endregion

    /// <summary>
    /// 计算K线开始时间和结束时间
    /// </summary>
    /// <param name="cycle"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    public (DateTimeOffset start, DateTimeOffset end) KlineTime(E_KlineType cycle, DateTimeOffset time)
    {
        DateTimeOffset start = time;
        DateTimeOffset end = time;
        switch (cycle)
        {
            case E_KlineType.min1:
                start = time.AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
                end = start.AddMinutes(1).AddMilliseconds(-1);
                break;
            case E_KlineType.min5:
                start = FactoryService.instance.system_init.AddMinutes((int)(time - FactoryService.instance.system_init).TotalMinutes / 5 * 5);
                end = start.AddMinutes(5).AddMilliseconds(-1);
                break;
            case E_KlineType.min15:
                start = FactoryService.instance.system_init.AddMinutes((int)(time - FactoryService.instance.system_init).TotalMinutes / 15 * 15);
                end = start.AddMinutes(15).AddMilliseconds(-1);
                break;
            case E_KlineType.min30:
                start = FactoryService.instance.system_init.AddMinutes((int)(time - FactoryService.instance.system_init).TotalMinutes / 30 * 30);
                end = start.AddMinutes(30).AddMilliseconds(-1);
                break;
            case E_KlineType.hour1:
                start = time.AddMinutes(-time.Minute).AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
                end = start.AddHours(1).AddMilliseconds(-1);
                break;
            case E_KlineType.hour6:
                start = FactoryService.instance.system_init.AddHours((int)(time - FactoryService.instance.system_init).TotalHours / 6 * 6);
                end = start.AddHours(6).AddMilliseconds(-1);
                break;
            case E_KlineType.hour12:
                start = FactoryService.instance.system_init.AddHours((int)(time - FactoryService.instance.system_init).TotalHours / 12 * 12);
                end = start.AddHours(12).AddMilliseconds(-1);
                break;
            case E_KlineType.day1:
                start = time.AddHours(-time.Hour).AddMinutes(-time.Minute).AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
                end = start.AddDays(1).AddMilliseconds(-1);
                break;
            case E_KlineType.week1:
                start = FactoryService.instance.system_init.AddDays((int)(time - FactoryService.instance.system_init).TotalDays / 7 * 7);
                end = start.AddDays(7).AddMilliseconds(-1);
                break;
            case E_KlineType.month1:
                start = time.AddDays(-time.Day).AddHours(-time.Hour).AddMinutes(-time.Minute).AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
                end = start.AddMonths(1).AddMilliseconds(-1);
                break;
            default:
                break;
        }
        return (start, end);
    }

    /// <summary>
    /// 交易记录转换成一条K线
    /// </summary>
    /// <param name="cycle">k线类型</param>
    /// <param name="start">开始时间</param>
    /// <param name="deals">交易记录</param>
    /// <returns></returns>
    public Kline? DealToKline(E_KlineType cycle, DateTimeOffset start, List<Deal> deals)
    {
        if (deals == null || deals.Count == 0)
        {
            return null;
        }
        var min1 = from deal in deals
                   group deal by new { deal.market, deal.symbol } into g
                   select new Kline
                   {
                       id = FactoryService.instance.constant.worker.NextId(),
                       market = g.Key.market,
                       symbol = g.Key.symbol,
                       type = cycle,
                       amount = g.Sum(x => x.amount),
                       count = g.Count(),
                       total = g.Sum(x => x.total),
                       open = g.First().price,
                       close = g.Last().price,
                       high = g.Max(x => x.price),
                       low = g.Min(x => x.price),
                       time_start = start,
                       time_end = g.Last().time,
                       time = DateTimeOffset.UtcNow,
                   };
        return min1.FirstOrDefault();
    }

    /// <summary>
    /// 删除redis kline数据
    /// </summary>
    /// <param name="market">交易对</param>
    public void DeleteRedisKline(long market)
    {
        FactoryService.instance.constant.redis.KeyDelete(FactoryService.instance.GetRedisKlineing(market));
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            FactoryService.instance.constant.redis.KeyDelete(FactoryService.instance.GetRedisKline(market, cycle));
        }
    }



}