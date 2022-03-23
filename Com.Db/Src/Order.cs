using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Com.Db.Enum;

namespace Com.Db;

/// <summary>
/// 订单
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>
public class Orders
{
    /// <summary>
    /// 订单id
    /// </summary>
    /// <value></value>
    public long order_id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
    /// <summary>
    /// 交易对名称
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 客户自定义订单id
    /// </summary>
    /// <value></value>
    public string? client_id { get; set; } = null;
    /// <summary>
    /// 用户ID
    /// </summary>
    /// <value></value>
    public long uid { get; set; }
    /// <summary>
    /// 挂单价
    /// </summary>
    /// <value></value>
    public decimal price { get; set; }
    /// <summary>
    /// 挂单量
    /// </summary>
    /// <value></value>
    public decimal amount { get; set; }
    /// <summary>
    /// 订单总额
    /// </summary>
    /// <value></value>
    public decimal total { get; set; }
    /// <summary>
    /// 挂单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset create_time { get; set; }
    /// <summary>
    /// 未成交量
    /// </summary>
    /// <value></value>
    public decimal amount_unsold { get; set; }
    /// <summary>
    /// 已成交量
    /// </summary>
    /// <value></value>
    public decimal amount_done { get; set; }
    /// <summary>
    /// 最后成交时间或撤单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset? deal_last_time { get; set; }
    /// <summary>
    /// 交易方向
    /// </summary>
    /// <value></value>
    public E_OrderSide side { get; set; }
    /// <summary>
    /// 订单状态
    /// </summary>
    /// <value></value>
    public E_OrderState state { get; set; }
    /// <summary>
    /// 订单类型
    /// </summary>
    /// <value></value>
    public E_OrderType type { get; set; }
    /// <summary>
    /// 手续费率
    /// </summary>
    /// <value></value>
    public decimal fee_rate { get; set; }
    /// <summary>
    /// 触发挂单价格
    /// </summary>
    /// <value></value>   
    public decimal trigger_hanging_price { get; set; }
    /// <summary>
    /// 触发撤单价格
    /// </summary>
    /// <value></value>    
    [JsonIgnore]
    public decimal trigger_cancel_price { get; set; }
    /// <summary>
    /// 附加数据
    /// </summary>
    /// <value></value>
    public string? data { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>
    public string? remarks { get; set; }
}