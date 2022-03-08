using System;
using System.Collections.Concurrent;
using Com.Model;
using Com.Model.Enum;
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
    /// <summary>
    /// 成交记录队列
    /// </summary>
    /// <param name="deal"></param>
    /// <param name="cancel"></param>
    public ConcurrentQueue<List<MatchDeal>> deal_queue { get; set; } = null!;
    /// <summary>
    /// 撤单记录队列
    /// </summary>
    /// <value></value>
    public ConcurrentQueue<List<MatchOrder>> cancel_queue { get; set; } = null!;
    /// <summary>
    /// 最近K线
    /// </summary>
    /// <typeparam name="E_KlineType">K线类型</typeparam>
    /// <typeparam name="Kline">K线</typeparam>
    /// <returns></returns>
    public Dictionary<E_KlineType, BaseKline> kline = new Dictionary<E_KlineType, BaseKline>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="info"></param>
    public MatchModel(BaseMarketInfo info)
    {
        this.info = info;
        this.deal_queue = new ConcurrentQueue<List<MatchDeal>>();
        this.cancel_queue = new ConcurrentQueue<List<MatchOrder>>();
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            this.kline.Add(cycle, new BaseKline()
            {
                market = this.info.market,
                type = cycle,
            });
        }
    }
}