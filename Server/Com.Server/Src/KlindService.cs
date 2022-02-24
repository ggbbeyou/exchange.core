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
        this.kilneHelper = new KilneHelper(this.constant);
    }

    /// <summary>
    /// 预热缓存
    /// </summary>
    /// <param name="market"></param>
    public void DBtoRedis(string market)
    {
        string key = string.Format(this.redis_key_kline, market, E_KlineType.min1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
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
            this.constant.redis.SortedSetAdd(key, entries);
            this.kilneHelper.SaveKline(market, E_KlineType.min1, klines);
        }
        List<BaseKline> klines_temp = new List<BaseKline>();
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            if (cycle == E_KlineType.min1)
            {
                continue;
            }
            klines_temp.Clear();
            // DateTimeOffset max_other = GetRedisLastKline(market, cycle);
            // TimeSpan span_other = TimeAdd(max, cycle);
            // max_other = max_other.Add(span_other);
            // RedisValue[] value = this.constant.redis.SortedSetRangeByScore(key, max_other.ToUnixTimeSeconds(), double.PositiveInfinity, Exclude.Stop, StackExchange.Redis.Order.Ascending);
            // foreach (var item in value)
            // {
            //     if (item.IsNullOrEmpty)
            //     {
            //         continue;
            //     }
            //     klines_temp.Add(JsonConvert.DeserializeObject<BaseKline>(item)!);
            //     List<BaseKline> klines1 = MergeKline(klines_temp, cycle);
            //     this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, cycle), klines1.Select(x => new SortedSetEntry(JsonConvert.SerializeObject(x), x.time_start.ToUnixTimeSeconds())).ToArray());
            // }
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
        string key = string.Format(this.redis_key_kline, market, klineType);
        RedisValue[] redisvalue = this.constant.redis.SortedSetRangeByRank(key, 0, 1, StackExchange.Redis.Order.Descending);
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

    public List<BaseKline> MergeKline(List<BaseKline> klines, E_KlineType klineType)
    {
        List<BaseKline> resutl = new List<BaseKline>();

        return resutl;
    }

}