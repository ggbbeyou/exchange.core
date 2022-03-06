using System;
using Com.Model;
using Com.Model.Enum;
using Com.Service.Match;

namespace Com.Service.Models;

/// <summary>
/// 成交单
/// </summary>
public class MatchModel
{
    /// <summary>
    /// 是否运行
    /// </summary>
    /// <value></value>
    public virtual bool run { get; set; }
    /// <summary>
    /// 交易对名称
    /// </summary>
    /// <value></value>
    public BaseMarketInfo info { get; set; } = null!;
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

}