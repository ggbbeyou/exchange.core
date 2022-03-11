using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Com.Db.Enum;

namespace Com.Db;

/// <summary>
/// 成交单
/// </summary>
public class Deal
{
    /// <summary>
    /// 成交id
    /// </summary>
    /// <value></value>
    public long trade_id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
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
    /// 买单id
    /// </summary>
    /// <value></value>
    public long bid_id { get; set; }
    /// <summary>
    /// 卖单id
    /// </summary>
    /// <value></value>
    public long ask_id { get; set; }
    /// <summary>
    /// 成交时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }
    /// <summary>
    /// 买订单
    /// </summary>
    /// <value></value>
    [NotMapped]
    [JsonIgnore]
    public Orders bid { get; set; } = null!;
    /// <summary>
    /// 卖订单
    /// </summary>
    /// <value></value>
    [NotMapped]
    [JsonIgnore]
    public Orders ask { get; set; } = null!;
}