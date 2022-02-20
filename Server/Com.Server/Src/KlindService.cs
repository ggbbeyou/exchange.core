using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Com.Common;
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
redis key
kline/name/E_KlineType



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
    /// redis 键  zset
    /// </summary>
    /// <value></value>
    public string redis_key_kline = "kline/{0}/{1}";

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public KlindService(FactoryConstant constant)
    {
        this.constant = constant;
    }

    /// <summary>
    /// 从redis获取最大的K线时间
    /// </summary>
    /// <param name="name">交易对</param>
    /// <param name="klineType">K线类型</param>
    /// <returns></returns>
    private DateTimeOffset GetRedisMaxMinuteKline(string name, E_KlineType klineType)
    {
        string key = string.Format(redis_key_kline, name, klineType);
        SortedSetEntry[] redisvalue = this.constant.redis.SortedSetRangeByRankWithScores(key, 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)redisvalue[0].Score);
        }
        return DateTimeOffset.MinValue;
    }

    private List<Kline> GetDB()
    {

    }



}