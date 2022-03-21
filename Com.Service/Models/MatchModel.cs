using System;
using System.Collections.Concurrent;
using Com.Db;
using Com.Db.Enum;
using Com.Service.Match;
using Microsoft.Extensions.Logging;

namespace Com.Service.Models;

/// <summary>
/// 撮合服务
/// </summary>
public class MatchModel
{
    /// <summary>
    /// 是否运行
    /// </summary>
    /// <value></value>
    public bool run { get; set; }
    /// <summary>
    /// 日志事件ID
    /// </summary>
    public EventId eventId;
    /// <summary>
    /// 交易对基本信息
    /// </summary>
    /// <value></value>
    public MarketInfo info { get; set; } = null!;
    /// <summary>
    /// 撮合服务
    /// </summary>
    /// <value></value>
    public MatchCore match_core { get; set; } = null!;
    /// <summary>
    /// 消息队列服务
    /// </summary>
    /// <value></value>
    public MQ mq { get; set; } = null!;
    /// <summary>
    /// 核心服务
    /// </summary>
    /// <value></value>
    public Core core { get; set; } = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="info"></param>
    public MatchModel(MarketInfo info)
    {
        this.info = info;
        this.eventId = new EventId(1, info.symbol);
    }
}