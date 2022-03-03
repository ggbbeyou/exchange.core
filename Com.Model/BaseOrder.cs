using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// 订单表
/// </summary>
public class BaseOrder
{
    /// <summary>
    /// 订单id
    /// </summary>
    /// <value></value>
    public long order_id { get; set; }
    /// <summary>
    /// 客户自定义订单id
    /// </summary>
    /// <value></value>
    public string? client_id { get; set; } = null;
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string market { get; set; } = null!;
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
    /// 触发撤单价格
    /// </summary>
    /// <value></value>
    public decimal trigger_cancel_price { get; set; }
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
    /// 已成交挂单量
    /// </summary>
    /// <value></value>
    public decimal amount_done { get; set; }
    /// <summary>
    /// 最后成交时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset? deal_last_time { get; set; }
    /// <summary>
    /// 交易方向
    /// </summary>
    /// <value></value>
    public E_OrderSide side { get; set; }
    /// <summary>
    /// 成交状态
    /// </summary>
    /// <value></value>
    public E_OrderState state { get; set; }
    /// <summary>
    /// 订单类型
    /// </summary>
    /// <value></value>
    public E_OrderType type { get; set; }
    /// <summary>
    /// 附加数据
    /// </summary>
    /// <value></value>
    public string? data { get; set; }

}