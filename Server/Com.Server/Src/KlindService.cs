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

namespace Com.Server;

/*


*/


/// <summary>
/// K线逻辑
/// </summary>
public class KlindService
{
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// redis(zset)键 已生成K线
    /// </summary>
    /// <value></value>
    public string redis_key_kline = "kline:{0}:{1}";
    /// <summary>
    /// redis(hash)键 正在生成K线
    /// </summary>
    /// <value></value>
    public string redis_key_klineing = "klineing:{0}:{1}";
    /// <summary>
    /// K线数据库操作
    /// </summary>
    public KilneHelper kilneHelper = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public KlindService(FactoryConstant constant)
    {
        this.constant = constant;
        this.kilneHelper = new KilneHelper(this.constant, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// 缓存预热
    /// </summary>
    /// <param name="market"></param>
    public void DBtoRedis(string market)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SyncMin1Kline(market, now);
        SyncKline(market, now);
    }

    /// <summary>
    /// 同步一分钟K线 deal转成kline，再分别保存到redis和db中。
    /// </summary>
    /// <param name="market"></param>
    public void SyncMin1Kline(string market, DateTimeOffset now)
    {

        BaseKline? last_kline = GetRedisLastKline(market, E_KlineType.min1);
        TimeSpan span = KlineTypeSpan(E_KlineType.min1);
        List<BaseKline> klines = this.kilneHelper.GetKlines(market, E_KlineType.min1, last_kline, new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, new TimeSpan()).AddMilliseconds(-1), span);
        if (klines != null && klines.Count() > 0)
        {
            SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
            for (int i = 0; i < klines.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i]), klines[i].time_start.ToUnixTimeSeconds());
            }
            this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, E_KlineType.min1), entries);
            this.kilneHelper.SaveKline(market, E_KlineType.min1, klines);
        }
    }

    /// <summary>
    /// 同步K线 从redis获取到1分钟K线,再转成其它K线,再分别保存到redis和db中。
    /// </summary>
    /// <param name="market"></param>
    public void SyncKline(string market, DateTimeOffset now)
    {
        List<BaseKline> klines_temp = new List<BaseKline>();
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            if (cycle == E_KlineType.min1 || cycle == E_KlineType.week1 || cycle == E_KlineType.month1)
            {
                continue;
            }
            klines_temp.Clear();
            BaseKline? last_kline = GetRedisLastKline(market, cycle);
            TimeSpan span = KlineTypeSpan(cycle);
            DateTimeOffset start = this.kilneHelper.system_init;
            decimal last_price = 0;
            if (last_kline != null)
            {
                start = last_kline.time_start.Add(span);
                last_price = last_kline.close;
            }
            RedisValue[] value = this.constant.redis.SortedSetRangeByScore(string.Format(this.redis_key_kline, market, E_KlineType.min1), start.ToUnixTimeSeconds(), double.PositiveInfinity, Exclude.Stop, StackExchange.Redis.Order.Ascending);
            foreach (var item in value)
            {
                if (item.IsNullOrEmpty)
                {
                    continue;
                }
                klines_temp.Add(JsonConvert.DeserializeObject<BaseKline>(item)!);
            }
            if (klines_temp.Count == 0)
            {
                continue;
            }
            List<BaseKline> klines = MergeKline(market, cycle, start, now, last_price, span, klines_temp);
            SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
            for (int i = 0; i < klines.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i]), klines[i].time_start.ToUnixTimeSeconds());
            }
            this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, cycle), entries);
            this.kilneHelper.SaveKline(market, cycle, klines);

        }
    }

    /// <summary>
    /// 从redis获取最大的K线时间
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="klineType">K线类型</param>
    /// <returns></returns>
    public BaseKline? GetRedisLastKline(string market, E_KlineType klineType)
    {
        RedisValue[] redisvalue = this.constant.redis.SortedSetRangeByRank(string.Format(this.redis_key_kline, market, klineType), 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return JsonConvert.DeserializeObject<BaseKline>(redisvalue[0]);
        }
        return null;
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
            case E_KlineType.hour4:
                return span = new TimeSpan(4, 0, 0);
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
    public List<BaseKline> MergeKline(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end, decimal last_price, TimeSpan span, List<BaseKline> klines)
    {
        List<BaseKline> resutl = new List<BaseKline>();

        for (DateTimeOffset i = start; i <= end; i = i.Add(span))
        {
            DateTimeOffset end_time = i.Add(span).AddMilliseconds(-1);
            List<BaseKline> deal = klines.Where(P => P.time_start >= i && P.time_end <= end_time).ToList();
            if (last_price == 0 && deal.Count == 0)
            {
                continue;
            }
            BaseKline baseKline = new BaseKline();
            baseKline.market = market;
            baseKline.type = klineType;
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

}