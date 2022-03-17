using System;
using System.Collections.Concurrent;
using Com.Db;
using Com.Db.Enum;
using Com.Service.Match;

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
    /// 成交记录队列
    /// </summary>
    /// <param name="deal"></param>
    /// <param name="cancel"></param>
    // public ConcurrentQueue<List<Deal>> deal_queue { get; set; } = null!;
    /// <summary>
    /// 撤单记录队列
    /// </summary>
    /// <value></value>
    // public ConcurrentQueue<List<Orders>> cancel_queue { get; set; } = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="info"></param>
    public MatchModel(MarketInfo info)
    {
        this.info = info;
        // this.deal_queue = new ConcurrentQueue<List<Deal>>();
        // this.cancel_queue = new ConcurrentQueue<List<Orders>>();
    }
}