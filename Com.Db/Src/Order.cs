using System;
using Com.Model;
using Com.Model.Enum;

namespace Com.Db;

/// <summary>
/// 订单
/// </summary>
public class Order : BaseOrder
{
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
    /// 最后成交时间或撤单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset? deal_last_time { get; set; }
    /// <summary>
    /// 订单状态
    /// </summary>
    /// <value></value>
    public E_OrderState state { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>
    public string? remarks { get; set; }
}