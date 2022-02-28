using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Com.Bll;
using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using StackExchange;
using StackExchange.Redis;

namespace Com.Bll;

/*


*/


/// <summary>
/// K线逻辑
/// </summary>
public class KlineService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly KlineService instance = new KlineService();
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// redis(zset)键 已生成K线 kline:btc/usdt:main1
    /// </summary>
    /// <value></value>
    public string redis_key_kline = "kline:{0}:{1}";
    /// <summary>
    /// redis(hash)键 正在生成K线
    /// </summary>
    /// <value></value>
    public string redis_key_klineing = "klineing:{0}:{1}";
    /// <summary>
    /// k线DB类
    /// </summary>
    public KilneHelper kilneHelper = null!;
    /// <summary>
    /// 交易记录Db类
    /// </summary>
    public DealHelper dealHelper = null!;
    /// <summary>
    /// 系统初始化时间  初始化  注:2017-1-1 此时是一年第一天，一年第一月，一年第一个星期日(星期日是一个星期开始的第一天)
    /// </summary>
    public DateTimeOffset system_init;

    /// <summary>
    ///
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    private KlineService()
    {

    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public void Init(FactoryConstant constant, DateTimeOffset system_init)
    {
        this.system_init = system_init;
        this.constant = constant;
        this.dealHelper = new DealHelper(constant);
        this.kilneHelper = new KilneHelper(constant, system_init);
    }

    #region 已确定K线

    /// <summary>
    /// 缓存预热(已确定K线)
    /// </summary>
    /// <param name="market"></param>
    /// <param name="end">同步到结束时间</param>
    public void DBtoRedised(List<string> markets, DateTimeOffset end)
    {
        foreach (var market in markets)
        {
            SyncDealToKlineMin1(market, end);
            SyncKlines(market, end);
            DbSaveRedis(market);
        }
    }

    /// <summary>
    /// 在DB中,由Deal转成1分钟K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="end"></param>
    public void SyncDealToKlineMin1(string market, DateTimeOffset end)
    {
        Kline? last_kline = this.kilneHelper.GetLastKline(market, E_KlineType.min1);
        List<Kline> klines = this.kilneHelper.GetKlineMin(market, end, last_kline);
        int min1_count = this.kilneHelper.SaveKline(market, E_KlineType.min1, klines);
    }

    /// <summary>
    /// 在DB里保存低频K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="now"></param>
    public void SyncKlines(string market, DateTimeOffset now)
    {
        E_KlineType previous_type = E_KlineType.min1;
        Kline? last_kline = null;
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            if (cycle == E_KlineType.min1)
            {
                previous_type = E_KlineType.min1;
                continue;
            }
            last_kline = this.kilneHelper.GetLastKline(market, cycle);
            List<Kline> klines = this.kilneHelper.GetKlines(market, previous_type, cycle, last_kline?.time_end ?? this.system_init, now);
            int count = this.kilneHelper.SaveKline(market, cycle, klines);
            if (cycle == E_KlineType.month1)
            {
                previous_type = E_KlineType.day1;
            }
            else
            {
                previous_type = cycle;
            }
        }
    }

    /// <summary>
    /// 将DB中的K线数据保存到Redis
    /// </summary>
    /// <param name="market"></param>
    public void DbSaveRedis(string market)
    {
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            if (cycle == E_KlineType.min1)
            {
                continue;
            }
            Kline? Last_kline = GetRedisLastKline(market, cycle);
            List<Kline> klines = this.kilneHelper.GetKlines(market, cycle, Last_kline?.time_end ?? this.system_init, DateTimeOffset.Now);
            if (klines.Count() > 0)
            {
                SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
                for (int i = 0; i < klines.Count(); i++)
                {
                    entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i]), klines[i].time_start.ToUnixTimeMilliseconds());
                }
                this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, E_KlineType.min1), entries);
            }
        }
    }

    #endregion


    #region 未确定K线
    /// <summary>
    /// 缓存预热(未确定K线)
    /// </summary>
    /// <param name="market"></param>
    /// <param name="end">同步到结束时间</param>
    public void DBtoRedising(List<string> markets, DateTimeOffset end)
    {

    }

    #endregion















    /// <summary>
    /// 从redis获取最大的K线时间
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="klineType">K线类型</param>
    /// <returns></returns>
    public Kline? GetRedisLastKline(string market, E_KlineType klineType)
    {
        RedisValue[] redisvalue = this.constant.redis.SortedSetRangeByRank(string.Format(this.redis_key_kline, market, klineType), 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return JsonConvert.DeserializeObject<Kline>(redisvalue[0]);
        }
        return null;
    }

    /// <summary>
    /// 合并K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="last_price"></param>
    /// <param name="span"></param>
    /// <param name="klines"></param>
    /// <returns></returns>
    public List<Kline> MergeKline(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end, decimal last_price, TimeSpan span, List<Kline> klines)
    {
        List<Kline> resutl = new List<Kline>();
        klines = klines.OrderBy(P => P.time_start).ToList();
        for (DateTimeOffset i = start; i <= end; i = i.Add(span))
        {
            DateTimeOffset end_time = i.Add(span).AddMilliseconds(-1);
            List<Kline> deal = klines.Where(P => P.time_start >= i && P.time_end <= end_time).ToList();
            if (last_price == 0 && deal.Count == 0)
            {
                continue;
            }
            Kline baseKline = new Kline();
            baseKline.market = market;
            baseKline.type = klineType;
            baseKline.amount = 0;
            baseKline.count = 0;
            baseKline.total = 0;
            baseKline.open = last_price;
            baseKline.close = last_price;
            baseKline.high = last_price;
            baseKline.low = last_price;
            baseKline.time_start = i;
            baseKline.time_end = end_time;
            baseKline.time = DateTimeOffset.UtcNow;
            if (deal.Count > 0)
            {
                baseKline.amount = deal.Sum(P => P.amount);
                baseKline.count = deal.Sum(P => P.count);
                baseKline.total = deal.Sum(P => P.total);
                baseKline.open = deal.First().open;
                baseKline.close = deal.Last().close;
                baseKline.high = deal.Max(P => P.high);
                baseKline.low = deal.Min(P => P.low);
            }
            last_price = baseKline.close;
            resutl.Add(baseKline);
        }
        return resutl;
    }









































    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    /// <summary>
    /// 同步一分钟K线 deal转成kline，再分别保存到redis和db中。
    /// </summary>
    /// <param name="market"></param>
    public void SyncMin1Kline1(string market, DateTimeOffset now)
    {
        Kline? last_kline = GetRedisLastKline(market, E_KlineType.min1);
        TimeSpan span = KlineTypeSpan(E_KlineType.min1);
        List<Kline> klines = this.kilneHelper.GetKlines(market, E_KlineType.min1, last_kline, new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, new TimeSpan()).AddMilliseconds(-1), span);
        if (klines.Count() > 0)
        {
            SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
            for (int i = 0; i < klines.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i]), klines[i].time_start.ToUnixTimeMilliseconds());
            }
            this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, E_KlineType.min1), entries);
            this.kilneHelper.SaveKline(market, E_KlineType.min1, klines);
        }
    }

    /// <summary>
    /// 同步K线 从redis获取到1分钟K线,再转成其它K线,再分别保存到redis和db中。
    /// </summary>
    /// <param name="market"></param>
    public void SyncKline1(string market, DateTimeOffset now)
    {
        List<Kline> klines_temp = new List<Kline>();
        E_KlineType previous = E_KlineType.min1;
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            if (cycle == E_KlineType.min1 || cycle == E_KlineType.week1 || cycle == E_KlineType.month1)
            {
                continue;
            }
            this.constant.logger.LogTrace(cycle.ToString());
            klines_temp.Clear();
            Kline? last_kline = GetRedisLastKline(market, cycle);
            TimeSpan span = KlineTypeSpan(cycle);
            DateTimeOffset start = this.system_init;
            decimal last_price = 0;
            if (last_kline != null)
            {
                start = last_kline.time_start.Add(span);
                last_price = last_kline.close;
            }
            RedisValue[] value = this.constant.redis.SortedSetRangeByScore(string.Format(this.redis_key_kline, market, previous), start.ToUnixTimeMilliseconds(), double.PositiveInfinity, Exclude.Stop, StackExchange.Redis.Order.Ascending);
            previous = cycle;
            foreach (var item in value)
            {
                if (item.IsNullOrEmpty)
                {
                    continue;
                }
                klines_temp.Add(JsonConvert.DeserializeObject<Kline>(item)!);
            }
            if (last_price == 0 && klines_temp.Count == 0)
            {
                continue;
            }
            else
            {
                start = klines_temp.Last().time_start;
            }
            List<Kline> klines = MergeKline(market, cycle, start, now, last_price, span, klines_temp);
            SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
            for (int i = 0; i < klines.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i]), klines[i].time_start.ToUnixTimeMilliseconds());
            }
            this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, cycle), entries);
            this.kilneHelper.SaveKline(market, cycle, klines);
        }
    }



    /// <summary>
    /// K线类型间隔时长
    /// </summary>
    /// <param name="klineType"></param>
    /// <returns></returns>
    public TimeSpan KlineTypeSpan(E_KlineType klineType)
    {
        TimeSpan span = new TimeSpan();
        switch (klineType)
        {
            case E_KlineType.min1:
                return span = new TimeSpan(0, 1, 0);
            case E_KlineType.min5:
                return span = new TimeSpan(0, 5, 0);
            case E_KlineType.min15:
                return span = new TimeSpan(0, 15, 0);
            case E_KlineType.min30:
                return span = new TimeSpan(0, 30, 0);
            case E_KlineType.hour1:
                return span = new TimeSpan(1, 0, 0);
            case E_KlineType.hour12:
                return span = new TimeSpan(12, 0, 0);
            case E_KlineType.day1:
                return span = new TimeSpan(24, 0, 0);
            case E_KlineType.week1:
                return span = new TimeSpan(168, 0, 0);
            case E_KlineType.month1:
                return span = new TimeSpan(720, 0, 0);
            default:
                return span;
        }
    }



}