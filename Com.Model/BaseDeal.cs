using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// 成交单
/// </summary>
public class BaseDeal
{
    /// <summary>
    /// 成交id
    /// </summary>
    /// <value></value>
    public string trade_id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string market { get; set; }
    /// <summary>
    /// 成交价
    /// </summary>
    /// <value></value>
    public decimal price { get; set; }
    /// <summary>
    /// 成交量
    /// </summary>
    /// <value></value>
    public decimal amount { get; set; }
    /// <summary>
    /// 成交总额
    /// </summary>
    /// <value></value>
    public decimal total { get; set; }
    /// <summary>
    /// 成交触发方向
    /// </summary>
    /// <value></value>
    public E_OrderSide trigger_side { get; set; }
    /// <summary>
    /// 成交时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }
    /// <summary>
    /// 买订单
    /// </summary>
    /// <value></value>
    public BaseOrder bid { get; set; }
    /// <summary>
    /// 卖订单
    /// </summary>
    /// <value></value>
    public BaseOrder ask { get; set; }
}