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
    /// redis(hash)键 正在生成K线 klineing:btc/usdt
    /// </summary>
    /// <value></value>
    public string redis_key_klineing = "klineing:{0}";
    /// <summary>
    /// k线DB类
    /// </summary>
    public KilneHelper kilneHelper = null!;

    /// <summary>
    /// 系统初始化时间  初始化  注:2017-1-1 此时是一年第一天，一年第一月，一年第一个星期日(星期日是一个星期开始的第一天)
    /// </summary>   
    public DateTimeOffset system_init = new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// private构造方法
    /// </summary>
    private KlineService()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    public void Init(FactoryConstant constant)
    {
        this.constant = constant;
        this.kilneHelper = new KilneHelper(constant);
    }

    #region 已确定K线

    /// <summary>
    /// 缓存预热(已确定K线)
    /// </summary>
    /// <param name="markets">交易对</param>
    /// <param name="end">结束时间</param>
    public void DBtoRedised(string market, DateTimeOffset end)
    {
        SyncKlines(market, end);
        DbSaveRedis(market);
    }

    /// <summary>
    /// 将K线保存到Db中
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="end">结束时间</param>
    public void SyncKlines(string market, DateTimeOffset end)
    {
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            Kline? last_kline = this.kilneHelper.GetLastKline(market, cycle);
            List<Kline>? klines = this.kilneHelper.CalcKlines(market, cycle, last_kline?.time_end ?? this.system_init, end);
            if (klines != null)
            {
                int count = this.kilneHelper.SaveKline(market, cycle, klines);
            }
        }
    }

    /// <summary>
    /// 将DB中的K线数据保存到Redis
    /// </summary>
    /// <param name="market">交易对</param>
    public void DbSaveRedis(string market)
    {
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            Kline? Last_kline = GetRedisLastKline(market, cycle);
            List<Kline> klines = this.kilneHelper.GetKlines(market, cycle, Last_kline?.time_end ?? this.system_init, DateTimeOffset.Now);
            if (klines.Count() > 0)
            {
                SortedSetEntry[] entries = new SortedSetEntry[klines.Count()];
                for (int i = 0; i < klines.Count(); i++)
                {
                    entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(klines[i]), klines[i].time_start.ToUnixTimeMilliseconds());
                }
                this.constant.redis.SortedSetAdd(string.Format(this.redis_key_kline, market, cycle), entries);
            }
        }
    }

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

    #endregion


    #region 未确定K线

    /// <summary>
    /// 缓存预热(未确定K线)
    /// </summary>
    /// <param name="market">交易对</param>
    public void DBtoRedising(string market)
    {
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            DateTimeOffset start = this.system_init;
            Kline? kline_last = this.kilneHelper.GetLastKline(market, cycle);
            if (kline_last != null)
            {
                start = kline_last.time_end.AddMilliseconds(1);
            }
            Kline? kline_new = DealService.instance.dealHelper.GetKlinesByDeal(market, cycle, start, null);
            if (kline_new != null)
            {
                this.constant.redis.HashSet(string.Format(this.redis_key_klineing, market), cycle.ToString(), JsonConvert.SerializeObject(kline_new));
            }
        }
    }

    #endregion
}